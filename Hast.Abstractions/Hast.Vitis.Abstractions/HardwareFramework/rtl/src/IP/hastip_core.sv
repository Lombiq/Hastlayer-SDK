// default_nettype of none prevents implicit wire declaration.
`default_nettype none
module hastip_core #(
  parameter integer C_M00_AXI_ADDR_WIDTH = 64 ,
  parameter integer C_M00_AXI_DATA_WIDTH = 32
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
  // Control Signals
  input  wire                              ap_start       ,
  output reg                               ap_idle        ,
  output reg                               ap_done        ,
  output reg                               ap_ready       ,
  input  wire [64-1:0]                     axi00_ptr0     
);

timeunit 1ps;
timeprecision 1ps;

///////////////////////////////////////////////////////////////////////////////
// Local Parameters
///////////////////////////////////////////////////////////////////////////////

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

parameter HAST_IP_DATA_WIDTH = 32;
parameter HAST_IP_MEMBER_ID = 0;

reg [HAST_IP_DATA_WIDTH-1:0] hastipDataOutFsm;
reg [31:0] hastipCellIndexFsm;
reg hastipReadEnableFsm;
reg hastipWriteEnableFsm;

wire [HAST_IP_DATA_WIDTH-1:0] hastipDataOutIp;
wire [31:0] hastipCellIndexIp;
wire hastipReadEnableIp;
wire hastipWriteEnableIp;

wire [HAST_IP_DATA_WIDTH-1:0] hastipDataOut;
wire [31:0] hastipCellIndex;
wire hastipReadEnable;
wire hastipWriteEnable;

reg [31:0] hastipMemberId;
reg hastipStarted;
wire hastipFinished;

wire [HAST_IP_DATA_WIDTH-1:0] hastipDataIn;
wire hastipReadsDone;
wire hastipWritesDone;

Hast_IP_Wrapper hastip
(
.DataIn      (hastipDataIn),
.DataOut     (hastipDataOutIp),
.CellIndex   (hastipCellIndexIp),
.ReadEnable  (hastipReadEnableIp),
.WriteEnable (hastipWriteEnableIp),
.ReadsDone   (hastipReadsDone),
.WritesDone  (hastipWritesDone),
.MemberId    (hastipMemberId),
.Reset       (areset),
.Started     (hastipStarted),
.Finished    (hastipFinished),
.Clock       (ap_clk)
);

reg [31:0] hastipBufferOffset;
reg [63:0] hastipTimer;

reg hastipSwitch = 0;

assign hastipDataOut     = (hastipSwitch == 0) ? hastipDataOutFsm     : hastipDataOutIp;
assign hastipCellIndex   = (hastipSwitch == 0) ? hastipCellIndexFsm   : hastipCellIndexIp + hastipBufferOffset;
assign hastipReadEnable  = (hastipSwitch == 0) ? hastipReadEnableFsm  : hastipReadEnableIp;
assign hastipWriteEnable = (hastipSwitch == 0) ? hastipWriteEnableFsm : hastipWriteEnableIp;

typedef enum {
  FSM_IDLE,
  INIT_1,
  INIT_2,
  INIT_3,
  INIT_4,
  GO_1,
  DONE_1,
  DONE_2,
  DONE_3,
  DONE_4,
  DONE_5,
  DONE_6,
  FSM_LAST
} FSM_State_Type;

FSM_State_Type fsm_state = FSM_IDLE;

always @(posedge ap_clk) begin
  if (areset) begin
    fsm_state <= FSM_IDLE;
  end
  else begin
    case (fsm_state)
    
      FSM_IDLE:
        begin
          ap_idle = 1;
          ap_done = 0;
          ap_ready = 1;
          hastipDataOutFsm <= 0;
          hastipCellIndexFsm <= 0;
          hastipReadEnableFsm <= 0;
          hastipWriteEnableFsm <= 0;
          hastipSwitch <= 0;
          hastipBufferOffset <= 0;
          hastipTimer <= 0;
          hastipMemberId = 0;
          hastipStarted = 0;
          if (ap_start) begin
            ap_idle = 0;
            ap_done = 0;
            ap_ready = 0;
            fsm_state <= INIT_1;
            $display("%0d: FSM: ap_start_pulse", $time);
          end
        end

      INIT_1:
        begin
          hastipReadEnableFsm <= 0;
          if (hastipReadsDone == 0) begin
            fsm_state <= INIT_2;
          end
        end
        
      INIT_2:
        begin
          hastipCellIndexFsm <= 0;
          hastipReadEnableFsm <= 1;
          if (hastipReadsDone) begin
            hastipBufferOffset <= hastipDataIn;
            hastipReadEnableFsm <= 0;
            fsm_state <= INIT_3;
            $display("%0d: FSM: hastipBufferOffset %d", $time, hastipDataIn);
          end
        end

      INIT_3:
        begin
          hastipReadEnableFsm <= 0;
          if (hastipReadsDone == 0) begin
            fsm_state <= INIT_4;
          end
        end

      INIT_4:
        begin
          hastipCellIndexFsm <= 1;
          hastipReadEnableFsm <= 1;
          if (hastipReadsDone) begin
            hastipMemberId <= hastipDataIn;
            hastipReadEnableFsm <= 0;
            fsm_state <= GO_1;
            $display("%0d: FSM: hastipMemberId %d", $time, hastipDataIn);
          end
        end

      GO_1:
        begin
          hastipTimer <= hastipTimer + 1;
          hastipStarted <= 1;
          hastipSwitch <= 1;
          if (hastipFinished) begin
            hastipStarted <= 0;
            hastipSwitch <= 0;
            fsm_state <= DONE_1;
            $display("%0d: FSM: hastipFinished", $time);
          end
        end

      DONE_1:
        begin
          hastipWriteEnableFsm <= 0;
          if (hastipWritesDone == 0) begin
            fsm_state <= DONE_2;
          end
        end
        
      DONE_2:
        begin
          hastipCellIndexFsm <= 2;
          hastipDataOutFsm <= hastipTimer[31:0];
          hastipWriteEnableFsm <= 1;
          if (hastipWritesDone) begin
            hastipWriteEnableFsm <= 0;
            fsm_state <= DONE_3;
            $display("%0d: FSM: hastipWriteEnableFsm hastipTimer[31:0] %x", $time, hastipTimer[31:0]);
          end
        end

      DONE_3:
        begin
          hastipWriteEnableFsm <= 0;
          if (hastipWritesDone == 0) begin
            fsm_state <= DONE_4;
          end
        end
        
      DONE_4:
        begin
          hastipCellIndexFsm <= 3;
          hastipDataOutFsm <= hastipTimer[63:32];
          hastipWriteEnableFsm <= 1;
          if (hastipWritesDone) begin
            hastipWriteEnableFsm <= 0;
            fsm_state <= DONE_5;
            $display("%0d: FSM: hastipWriteEnableFsm hastipTimer[63:32] %x", $time, hastipTimer[63:32]);
          end
        end

      DONE_5:
        begin
          ap_done <= 1'b1;
          fsm_state <= DONE_6;
        end

      DONE_6:
        begin
          ap_done <= 1'b0;
          fsm_state <= FSM_IDLE;
        end
          
        default:
          begin
            fsm_state <= FSM_IDLE;
          end
          
    endcase        
    
  end
end

hastip_core_cache #(
  .C_M00_AXI_ADDR_WIDTH ( C_M00_AXI_ADDR_WIDTH ),
  .C_M00_AXI_DATA_WIDTH ( C_M00_AXI_DATA_WIDTH ),
  .HAST_IP_DATA_WIDTH   ( HAST_IP_DATA_WIDTH )
)
inst_cache (
  .ap_clk          ( ap_clk          ),
  .ap_rst_n        ( ap_rst_n        ),
  .m00_axi_awvalid ( m00_axi_awvalid ),
  .m00_axi_awready ( m00_axi_awready ),
  .m00_axi_awaddr  ( m00_axi_awaddr  ),
  .m00_axi_awlen   ( m00_axi_awlen   ),
  .m00_axi_wvalid  ( m00_axi_wvalid  ),
  .m00_axi_wready  ( m00_axi_wready  ),
  .m00_axi_wdata   ( m00_axi_wdata   ),
  .m00_axi_wstrb   ( m00_axi_wstrb   ),
  .m00_axi_wlast   ( m00_axi_wlast   ),
  .m00_axi_bvalid  ( m00_axi_bvalid  ),
  .m00_axi_bready  ( m00_axi_bready  ),
  .m00_axi_arvalid ( m00_axi_arvalid ),
  .m00_axi_arready ( m00_axi_arready ),
  .m00_axi_araddr  ( m00_axi_araddr  ),
  .m00_axi_arlen   ( m00_axi_arlen   ),
  .m00_axi_rvalid  ( m00_axi_rvalid  ),
  .m00_axi_rready  ( m00_axi_rready  ),
  .m00_axi_rdata   ( m00_axi_rdata   ),
  .m00_axi_rlast   ( m00_axi_rlast   ),
  .hastipDataIn (hastipDataIn),
  .hastipDataOut (hastipDataOut),
  .hastipCellIndex (hastipCellIndex),
  .hastipReadEnable (hastipReadEnable),
  .hastipWriteEnable (hastipWriteEnable),
  .hastipReadsDone (hastipReadsDone),
  .hastipWritesDone (hastipWritesDone),
  .axi00_ptr0 (axi00_ptr0)
);

endmodule : hastip_core
`default_nettype wire
