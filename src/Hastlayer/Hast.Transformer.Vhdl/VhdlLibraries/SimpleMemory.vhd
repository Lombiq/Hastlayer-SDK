library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
        
package SimpleMemory is
    -- Data conversion functions:
    function ConvertUInt32ToStdLogicVector(input: unsigned(31 downto 0)) return std_logic_vector;
    function ConvertStdLogicVectorToUInt32(input : std_logic_vector) return unsigned;
        
    function ConvertBooleanToStdLogicVector(input: boolean) return std_logic_vector;
    function ConvertStdLogicVectorToBoolean(input : std_logic_vector) return boolean;
        
    function ConvertInt32ToStdLogicVector(input: signed(31 downto 0)) return std_logic_vector;
    function ConvertStdLogicVectorToInt32(input : std_logic_vector) return signed;
end SimpleMemory;
        
package body SimpleMemory is

    function ConvertUInt32ToStdLogicVector(input: unsigned(31 downto 0)) return std_logic_vector is
    begin
        return std_logic_vector(input);
    end ConvertUInt32ToStdLogicVector;
    
    function ConvertStdLogicVectorToUInt32(input : std_logic_vector) return unsigned is
    begin
        return unsigned(input);
    end ConvertStdLogicVectorToUInt32;
    
    function ConvertBooleanToStdLogicVector(input: boolean) return std_logic_vector is 
    begin
        case input is
            when true => return X"FFFFFFFF";
            when false => return X"00000000";
            when others => return X"00000000";
        end case;
    end ConvertBooleanToStdLogicVector;

    function ConvertStdLogicVectorToBoolean(input : std_logic_vector) return boolean is 
    begin
        -- In .NET a false is all zeros while a true is at least one 1 bit (or more), so using the same logic here.
        return not(input = X"00000000");
    end ConvertStdLogicVectorToBoolean;

    function ConvertInt32ToStdLogicVector(input: signed(31 downto 0)) return std_logic_vector is
    begin
        return std_logic_vector(input);
    end ConvertInt32ToStdLogicVector;

    function ConvertStdLogicVectorToInt32(input : std_logic_vector) return signed is
    begin
        return signed(input);
    end ConvertStdLogicVectorToInt32;

end SimpleMemory;
