// default_nettype of none prevents implicit wire declaration.
`default_nettype none
module hastip_core_cache #(
  parameter integer C_M00_AXI_ADDR_WIDTH = 64 ,
  parameter integer C_M00_AXI_DATA_WIDTH = 32 ,
  parameter integer HAST_IP_DATA_WIDTH   = 32
)
(
  // System Signals
  input  wire                              ap_clk         ,
  input  wire                              ap_rst_n       ,
  // AXI4 master interface m00_axi
  output reg                               m00_axi_awvalid = 0,
  input  wire                              m00_axi_awready,
  output reg  [C_M00_AXI_ADDR_WIDTH-1:0]   m00_axi_awaddr  = 0,
  output reg  [8-1:0]                      m00_axi_awlen   = 0,
  output reg                               m00_axi_wvalid  = 0,
  input  wire                              m00_axi_wready ,
  output reg  [C_M00_AXI_DATA_WIDTH-1:0]   m00_axi_wdata   = 0,
  output reg  [C_M00_AXI_DATA_WIDTH/8-1:0] m00_axi_wstrb   = 0,
  output reg                               m00_axi_wlast   = 0,
  input  wire                              m00_axi_bvalid ,
  output reg                               m00_axi_bready  = 0,
  output reg                               m00_axi_arvalid = 0,
  input  wire                              m00_axi_arready,
  output reg  [C_M00_AXI_ADDR_WIDTH-1:0]   m00_axi_araddr  = 0,
  output reg  [8-1:0]                      m00_axi_arlen   = 0,
  input  wire                              m00_axi_rvalid ,
  output reg                               m00_axi_rready  = 0,
  input  wire [C_M00_AXI_DATA_WIDTH-1:0]   m00_axi_rdata  ,
  input  wire                              m00_axi_rlast  ,
  // Hast_IP Signals
  output reg [HAST_IP_DATA_WIDTH-1:0] hastipDataIn,
  input wire [HAST_IP_DATA_WIDTH-1:0] hastipDataOut,
  input wire [31:0] hastipCellIndex,
  input wire hastipReadEnable,
  input wire hastipWriteEnable,
  output reg hastipReadsDone,
  output reg hastipWritesDone,
  input wire [64-1:0] axi00_ptr0
);

timeunit 1ps;
timeprecision 1ps;

///////////////////////////////////////////////////////////////////////////////
// Wires and Variables
///////////////////////////////////////////////////////////////////////////////
(* KEEP = "yes" *)
logic                                areset                         = 1'b0;

///////////////////////////////////////////////////////////////////////////////
// Begin RTL
///////////////////////////////////////////////////////////////////////////////

// Register and invert reset signal.
always @(posedge ap_clk) begin
  areset <= ~ap_rst_n;
end

    typedef enum {
        IDLE,
        WR_ADDR,
        WR_DATA,
        WR_DONE,
        RD_ADDR,
        RD_DATA,
        RD_DONE,
        LAST
    } AXI_State_Type;
    
    AXI_State_Type axi_state = IDLE;

    always @(posedge ap_clk) begin
        if (areset) begin
            axi_state = IDLE;
        end
        else begin
            case (axi_state)
            
                IDLE:
                    begin
                        hastipDataIn = 32'b0;
                        hastipReadsDone = 1'b0;
                        hastipWritesDone = 1'b0;

                        m00_axi_awvalid = 1'b0;
                        m00_axi_awaddr = 0;
                        m00_axi_awlen = 0;
                        m00_axi_wvalid = 1'b0;
                        m00_axi_wdata  = 0;
                        m00_axi_wstrb = 8'b11111111;
                        m00_axi_wlast = 1'b1;
                        m00_axi_bready = 1'b1;

                        m00_axi_arvalid = 1'b0;
                        m00_axi_araddr = 0;
                        m00_axi_arlen = 0;
                        m00_axi_rready = 1'b0;
                        
                        if (hastipWriteEnable) begin
                            $display("%0d: AXI: hastipWriteEnable, %x, %x", $time, hastipCellIndex, hastipDataOut);
                            axi_state = WR_ADDR;
                        end else if (hastipReadEnable) begin
                            $display("%0d: AXI: hastipReadEnable, %x", $time, hastipCellIndex);
                            axi_state = RD_ADDR;
                        end
                    end
                
                WR_ADDR:
                    begin
                        m00_axi_awaddr <= axi00_ptr0 + 4 * hastipCellIndex;
                        m00_axi_awvalid <= 1'b1;
                        if (m00_axi_awvalid && m00_axi_awready) begin
                            m00_axi_awvalid <= 1'b0;
                            axi_state <= WR_DATA;
                        end
                    end
                
                WR_DATA:
                    begin
                        m00_axi_wdata <= hastipDataOut;
                        m00_axi_wvalid <= 1'b1;
                        if (m00_axi_wvalid && m00_axi_wready) begin
                            m00_axi_wvalid <= 1'b0;
                            hastipWritesDone <= 1'b1;
                            axi_state <= WR_DONE;
                        end
                    end

                WR_DONE:
                    begin
                        if (hastipWriteEnable == 0) begin
                            hastipWritesDone <= 1'b0;
                            axi_state <= IDLE;
                        end
                    end

                RD_ADDR:
                    begin
                        m00_axi_araddr <= axi00_ptr0 + 4 * hastipCellIndex;
                        m00_axi_arvalid <= 1'b1;
                        if (m00_axi_arvalid && m00_axi_arready) begin
                            m00_axi_arvalid <= 1'b0;
                            axi_state <= RD_DATA;
                        end
                    end

                RD_DATA:
                    begin
                      if (m00_axi_rvalid) begin
                        m00_axi_rready <= 1'b1;
                        hastipDataIn = m00_axi_rdata;
                        hastipReadsDone <= 1'b1;
                        axi_state = RD_DONE;
                      end
                    end

                RD_DONE:
                    begin
                      m00_axi_rready <= 1'b0;
                      if (hastipReadEnable == 0) begin
                          hastipReadsDone = 1'b0;
                          axi_state = IDLE;
                      end
                    end
                
                default:
                    begin
                        axi_state = IDLE;
                    end
            endcase        
        end
    end

endmodule : hastip_core_cache
`default_nettype wire
