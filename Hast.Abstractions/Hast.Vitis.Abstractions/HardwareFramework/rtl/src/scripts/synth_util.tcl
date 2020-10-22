set vhdlname [lindex $::argv 0]
set rptname [lindex $::argv 0]

read_vhdl "${vhdlname}"
synth_design -top Hast_IP -part xcu250-figd2104-2L-e
report_utilization -file "${rptname}"
