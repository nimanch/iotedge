﻿ARG base_tag=6.0-alpine
FROM mcr.microsoft.com/dotnet/runtime:${base_tag}

ARG EXE_DIR=.

WORKDIR /app

COPY $EXE_DIR/ ./

CMD echo "$(date --utc +"[%Y-%m-%d %H:%M:%S %:z]"): Module is intended to be used with docker run command"
