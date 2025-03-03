#!/bin/bash
#the below will read the the CONN_STR from .env file - you may change this to your connection string
set -a
source .env
set +a

dotnet ef dbcontext scaffold $CONN_STR   Npgsql.EntityFrameworkCore.PostgreSQL  --output-dir EntityFramework   --context-dir .   --context "KahootContext" --no-onconfiguring  --force --schema kahoot 
