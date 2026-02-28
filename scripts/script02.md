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

clear && echo -e "Process\t\tPID\n-------\t\t---\ndotnet\t\t$DOTNET_PID\nAppHost\t\t$APPHOST_PID\nDCP\t\t$DCP_PID\nImageAPI\t$IMAGEAPI_PID"

#output a nice table that shows the process ids:
clear && echo -e "Process\t\tPID\n-------\t\t---\ndotnet\t\t$DOTNET_PID\nAppHost\t\t$APPHOST_PID\nDCP\t\t$DCP_PID\nDCPCTRL\t\t$DCPCTRL_PID\nImageAPI\t$IMAGEAPI_PID"

#show image api port (or dcpctrl forwarding it)
lsof -i :5062

#test the connection to the image api
curl 'http://localhost:5062/api/Question' 2>&1
clear

#output the ports opened by image api (not 5062)
nettop -p $IMAGEAPI_PID -l 1 -J state | grep tcp4

curl 'http://localhost:53982(replace)/api/Question' 2>&1

# clean up
clear && popd && killall dotnet




