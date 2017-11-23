#!/usr/bin/env bash

cd ../src/InEngine.Core/
nuget pack -properties Configuration=Release -properties Platform=AnyCPU

