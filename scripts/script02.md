# Demo which process launches which others
clear
pushd . && cd $(git rev-parse --show-toplevel)

## run Aspire AppHost project
aspire run --project src/AskVantage/Aspire/AskVantage.AppHost/AskVantage.AppHost.csproj &
##empty: dotnet run &
$RETURN

DOTNET_PID=$(ps | grep 'dotnet run.*AskVantage\.AppHost\.csproj' | awk '{print $1}')
APPHOST_PID=$(pgrep -P $DOTNET_PID | head -1)
DCP_PID=$(ps -f | grep $APPHOST_PID | grep "dcp start-apiserver" | awk '{print $2}')
DCPCTRL_PID=$(ps -f | grep $DCP_PID | grep "dcpctrl run-controllers" | awk '{print $2}')
IMAGEAPI_PID=$(ps -f | grep -v grep | grep -v "dotnet run" | grep ImageApi | awk '{print $2}')


#output a nice table that shows the process ids:
clear && echo -e "Process\t\tPID\n-------\t\t---\ndotnet\t\t$DOTNET_PID\nAppHost\t\t$APPHOST_PID\nDCP\t\t$DCP_PID\nDCPCTRL\t\t$DCPCTRL_PID\nImageAPI\t$IMAGEAPI_PID"


#test the connection to the image api
curl 'http://localhost:5062/api/Question' 2>&1
#clear

#output the ports opened by image api (not 5062)
#lsof -i -P -n -sTCP:LISTEN -a -p "$IMAGEAPI_PID" | grep 127.0.0.1: --color
nettop -p $IMAGEAPI_PID -l 1 -J state | grep tcp4

#output the ports opened by dcpctrl (child application proxy endpoints)
#lsof -i -P -n -a -p "$DCPCTRL_PID" | grep 5062 --color
nettop -p $DCPCTRL_PID -l 1 -J state | grep tcp4 | grep 5062 --color

# clean up
clear && popd && killall dotnet




