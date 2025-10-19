# run Aspire AppHost project
clear
pushd . && cd $(git rev-parse --show-toplevel)

## run Aspire AppHost project
aspire run --project src/AskVantage/Aspire/AskVantage.AppHost/AskVantage.AppHost.csproj &
$RETURN

# Get the frontend & api process ids
FRONTEND_PID=$(ps -f | grep -v grep | grep -v "dotnet run" | grep "AskVantage.Frontend" | awk '{print $2}')
IMAGEAPI_PID=$(ps -f | grep -v grep | grep -v "dotnet run" | grep ImageApi | awk '{print $2}')

clear && echo -e "Process\t\t\tPID\n-------\t\t\t---\nFrontend\t\t$FRONTEND_PID\nImageAPI\t\t$IMAGEAPI_PID"

# Show the 'services' env vars of that process
ps eww -p $FRONTEND_PID | grep -o "services__[^[:space:]]*" | sort
$RETURN

# Show the connection strings of the image api (contains COMPUTER VISION key)

ps eww -p $IMAGEAPI_PID | grep -o "COMPUTERVISION__E[^[:space:]]*" | sort
$RETURN

# the same variable name isn't on the frontend process
ps eww -p $FRONTEND_PID | grep -o "COMPUTERVISION__E[^[:space:]]*" | sort

killall dotnet && clear && popd