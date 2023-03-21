library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;

package TypeConversion is
    function SmartResize(input: unsigned; size: natural) return unsigned;
    function SmartResize(input: signed; size: natural) return signed;
    function ToUnsignedAndExpand(input: signed; size: natural) return unsigned;
end TypeConversion;
        
package body TypeConversion is

    -- The .NET behavior is different than that of resize() ("To create a larger vector, the new [leftmost] bit 
    -- positions are filled with the sign bit(ARG'LEFT). When truncating, the sign bit is retained along with the 
    -- rightmost part.") when casting to a smaller type: "If the source type is larger than the destination type, 
    -- then the source value is truncated by discarding its "extra" most significant bits. The result is then 
    -- treated as a value of the destination type." Thus we need to simply truncate when casting down. See:
    -- https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions
    function SmartResize(input: unsigned; size: natural) return unsigned is
    begin
        if (size < input'LENGTH) then
            return input(size - 1 downto 0);
        else
            -- Resize() is supposed to work with little endian numbers: "When truncating, the sign bit is retained
            -- along with the rightmost part." for signed numbers and "When truncating, the leftmost bits are 
            -- dropped." for unsigned ones. See: http://www.csee.umbc.edu/portal/help/VHDL/numeric_std.vhdl
            return resize(input, size);
        end if;
    end SmartResize;

    function SmartResize(input: signed; size: natural) return signed is
    begin
        if (size < input'LENGTH) then
            return input(size - 1 downto 0);
        else
            return resize(input, size);
        end if;
    end SmartResize;

    function ToUnsignedAndExpand(input: signed; size: natural) return unsigned is
        variable result: unsigned(size - 1 downto 0);
    begin
        if (input >= 0) then
            return resize(unsigned(input), size);
        else 
            result := (others => '1');
            result(input'LENGTH - 1 downto 0) := unsigned(input);
            return result;
        end if;
    end ToUnsignedAndExpand;

end TypeConversion;
