# run Aspire AppHost project
echo "skip this demo"
killall dotnet
# if you really want to:
echo "Manually start the debugger from Rider"

# Get the dotnet & app host process ids
RIDER_PID=$(ps -f | grep -v grep | grep Rider | grep -v DPA | head -1 | awk '{print $2}')
APPHOST_PID=$(pgrep -P $RIDER_PID | head -1)
DCP_PID=$(ps -f | grep $APPHOST_PID | grep "dcp start-apiserver" | awk '{print $2}')
DCPCTRL_PID=$(ps -f | grep $DCP_PID | grep "dcpctrl run-controllers" | awk '{print $2}')

# Show the 'DEBUG_' env vars of App Host process
ps eww -p $APPHOST_PID | grep -o "DEBUG_[^ ]*"

DEBUG_SESSION_PORT=$(ps eww -p $APPHOST_PID | grep -o "DEBUG_SESSION_PORT[^ ]*" | sed 's/.*localhost:\([0-9]*\).*/\1/')

# Show that the DCP is connected
lsof -i -P -n -sTCP:ESTABLISHED -a -p "$DCPCTRL_PID" | grep --color=always ":$DEBUG_SESSION_PORT"

# Show all connections from and to the debug port
lsof | grep ":$DEBUG_SESSION_PORT" --color=always

DOTNET_DEBUG_PID=$(lsof | grep ":$DEBUG_SESSION_PORT" | grep "dotnet" | head -1 | awk '{print $2}')
ps -f -p $DOTNET_DEBUG_PID