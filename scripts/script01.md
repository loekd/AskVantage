pushd . && cd $(git rev-parse --show-toplevel)

## run Aspire AppHost project
aspire run --project src/AskVantage/Aspire/AskVantage.AppHost/AskVantage.AppHost.csproj &
$RETURN

ps | grep -v grep | grep 'dotnet run.*AskVantage\.AppHost'
DOTNET_PID=$(ps | grep 'dotnet run.*AskVantage\.AppHost\.csproj' | awk '{print $1}')
pstree -p $DOTNET_PID | grep project
#echo "dotnet processid: $DOTNET_PID"
#clear

#output all processes that are related to dotnet run (should be the app host)
#ps -f | grep -v grep | grep $DOTNET_PID --color

#get process id of the apphost 'AskVantage.AppHost' which has dotnet as its parent
APPHOST_PID=$(pgrep -P $DOTNET_PID | head -1)

clear && echo "App Host processid: $APPHOST_PID"
#clear

#output all processes related to apphost process (should be dcp, monitoring apphost process):
ps -f | grep -v grep | grep $APPHOST_PID --color
DCP_PID=$(ps -f | grep $APPHOST_PID | grep "dcp start-apiserver" | awk '{print $2}')
clear && echo "DCP processid: $DCP_PID" && echo "App Host processid: $APPHOST_PID"

pstree -p $DCP_PID -l 3
DCPCTRL_PID=$(ps -f | grep $DCP_PID | grep "dcpctrl run-controllers" | awk '{print $2}')
clear && echo "DCPCTRL processid: $DCPCTRL_PID" && echo "DCP processid: $DCP_PID" && echo "App Host processid: $APPHOST_PID"

#output all processes related to dcp (should be dcpctrl, indirectly)
#ps -f | grep -v grep | grep $DCP_PID --color
pstree -p $DCPCTRL_PID -l 4

clear
#filter out dotnet
pstree -p $DCPCTRL_PID | grep dotnet --color

clear
#filter out docker
pstree -p $DCPCTRL_PID | grep 'docker events' --color

#show what docker events does:
docker events --filter type=container
$CTRL+C

clear
#filter out dcpproc
pstree -p $DCPCTRL_PID | grep dcpproc --color

# kill the dotnet process (which should kill all child processes as well)
kill $DOTNET_PID

# prove it
pstree -p $DCP_PID

# clean up
popd && killall dotnet && clear




