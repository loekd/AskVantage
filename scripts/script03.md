# Getting the details of the Developer Control Plane
clear
pushd . && cd $(git rev-parse --show-toplevel)

## run Aspire AppHost project
aspire run --project src/AskVantage/Aspire/AskVantage.AppHost/AskVantage.AppHost.csproj &
$RETURN

DOTNET_PID=$(ps | grep 'dotnet run.*AskVantage\.AppHost\.csproj' | awk '{print $1}')
APPHOST_PID=$(pgrep -P $DOTNET_PID | head -1)
DCP_PID=$(ps -f | grep $APPHOST_PID | grep "dcp start-apiserver" | awk '{print $2}')
IMAGEAPI_PID=$(ps -f | grep -v grep | grep -v "dotnet run" | grep ImageApi | awk '{print $2}')

clear && echo -e "Process\t\tPID\n-------\t\t---\ndotnet\t\t$DOTNET_PID\nAppHost\t\t$APPHOST_PID\nDCP\t\t$DCP_PID\nImageAPI\t$IMAGEAPI_PID"

# DCP is listening on  a port
nettop -p $DCP_PID -l 1 -J state

# AppHost is connected to DCP
nettop -p $APPHOST_PID -l 1 -J state

# how does the AppHost know where to find DCP?
## get the path to the kubeconfig file
clear && echo -e "Process\t\tPID\n-------\t\t---\ndotnet\t\t$DOTNET_PID\nAppHost\t\t$APPHOST_PID\nDCP\t\t$DCP_PID\nImageAPI\t$IMAGEAPI_PID"
pstree -p $DCP_PID -l 2 -w
KUBECONFIG=$(ps -f -p $DCP_PID | awk -F'--kubeconfig ' '{print $2}' | awk '{print $1}' | sort -u | tr -d '\n') && echo $KUBECONFIG
bat --language=yaml $KUBECONFIG -P
clear

## query the API
kubectl --kubeconfig $KUBECONFIG api-resources

## get executables and the name of the dashboard
kubectl --kubeconfig $KUBECONFIG get executables

IMAGEAPI=$(kubectl --kubeconfig $KUBECONFIG get executable | grep -v "dapr-" | awk '$1 ~ /^imageapi/ {print $1}')
#echo $IMAGEAPI
kubectl --kubeconfig $KUBECONFIG get executable $IMAGEAPI

kubectl --kubeconfig $KUBECONFIG get executable $IMAGEAPI -o yaml | head -22 | bat --language=yaml -P
clear

## run a second instance of the dashboard
cat ./scripts/dashboard.yaml | head -22 | bat --language=yaml -P
kubectl --kubeconfig $KUBECONFIG apply -f ./scripts/dashboard.yaml
$CTRL+C

kubectl --kubeconfig $KUBECONFIG get executable aspire-dashboard-2

## call it
curl -vk "http://localhost:18880/login?t=1b3b7218-9040-4b50-977d-dbbcb647ed3a"
# remove it
kubectl --kubeconfig $KUBECONFIG delete -f ./scripts/dashboard.yaml

## run a standalone Redis container
clear
cat ./scripts/container.yaml | head -22 | bat --language=yaml -P

lsof -i :6378

kubectl --kubeconfig $KUBECONFIG apply -f ./scripts/container.yaml
kubectl --kubeconfig $KUBECONFIG get container redis

lsof -i :6378
docker ps | grep redis:8.2

# output for next slide:
#kubectl --kubeconfig $KUBECONFIG get executable $IMAGEAPI -o yaml | grep "executionType" -C 1 | bat --language yaml -P

# clean up DCP by terminating the AppHost, so all children are removed as well.
kill $APPHOST_PID

# show that the container was removed
docker ps | grep redis:latest

popd && killall dotnet && clear




