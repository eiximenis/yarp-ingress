Push-Location ..\..\src
nerdctl build --namespace=k8s.io -t yarpingress -f YarpIngress\Dockerfile .
Pop-Location