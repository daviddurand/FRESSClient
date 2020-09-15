#!/bin/sh
MY_PATH=`dirname $0`
cd $MY_PATH/FressClient/bin/Debug/netcoreapp2.2
dotnet FressClient.dll $*
