-- Hast_IP.vhd wrapper for System Verilog integration
-- VHDL integer converted to std_logic_vector(31 downto 0)
-- VHDL boolean converted to std_logic

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;

entity Hast_IP_Wrapper is 
port(
  DataIn:      In std_logic_vector(31 downto 0);
  DataOut:     Out std_logic_vector(31 downto 0);
  CellIndex:   Out std_logic_vector(31 downto 0);
  ReadEnable:  Out std_logic;
  WriteEnable: Out std_logic;
  ReadsDone:   In std_logic;
  WritesDone:  In std_logic;
  MemberId:    In std_logic_vector(31 downto 0);
  Reset:       In std_logic;
  Started:     In std_logic;
  Finished:    Out std_logic;
  Clock:       In std_logic
);
end Hast_IP_Wrapper;

architecture Imp of Hast_IP_Wrapper is 

  -- outputs
  signal CellIndex_tmp : integer;
  signal ReadEnable_tmp : boolean;
  signal WriteEnable_tmp : boolean;
  signal Finished_tmp : boolean;

  -- inputs
  signal ReadsDone_tmp : boolean;
  signal WritesDone_tmp : boolean;
  signal MemberId_tmp : integer;
  signal Started_tmp : boolean;

begin 

  Hast_IP_inst : entity work.Hast_IP
  port map (
    \DataIn\      => DataIn,
    \DataOut\     => DataOut,
    \CellIndex\   => CellIndex_tmp,
    \ReadEnable\  => ReadEnable_tmp,
    \WriteEnable\ => WriteEnable_tmp,
    \ReadsDone\   => ReadsDone_tmp,
    \WritesDone\  => WritesDone_tmp,
    \MemberId\    => MemberId_tmp,
    \Reset\       => Reset,
    \Started\     => Started_tmp,
    \Finished\    => Finished_tmp,
    \Clock\       => Clock
  );

  -- outputs
  CellIndex <= std_logic_vector(to_unsigned(CellIndex_tmp, CellIndex'length));
  ReadEnable <= '1' when ReadEnable_tmp else '0';
  WriteEnable <= '1' when WriteEnable_tmp else '0';
  Finished <= '1' when Finished_tmp else '0';
    
  -- inputs
  ReadsDone_tmp <= (ReadsDone = '1');
  WritesDone_tmp <= (WritesDone = '1');
  MemberId_tmp <= to_integer(unsigned(MemberId));
  Started_tmp <= (Started = '1');

end Imp;
