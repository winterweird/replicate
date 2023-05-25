FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /src
COPY REPLicate.TransformUsings/ .
RUN dotnet build -c Release -o /out

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS runtime

# Ensure the csharprepl tool is installed. It will be installed in
# $HOME/.dotnet/tools/csharprepl, with $HOME for the root user being /root.
# We won't bother to put it in the path since the path won't be re-sourced
# anyway :)
RUN dotnet tool install -g csharprepl

ARG CFG=/repl
ARG CWD=/app

WORKDIR $CFG

# Default env variables (these are used in entrypoint.sh)
ENV REPL_LIBS_PATH=/libs
ENV REPL_RSP_FILE=$CFG/repl.rsp
ENV REPL_SCRIPT_FILE=$CFG/repl.csx
ENV REPL_RSP_PREPROCESSING_CMD="dotnet $CFG/scripts/REPLicate.TransformUsings.dll"

# Add entrypoint
COPY entrypoint.sh $CFG/entrypoint.sh

COPY --from=build /out ./scripts

# Ensure everything exists and has the right permissions
RUN chmod +x $CFG/entrypoint.sh && \
    touch $REPL_RSP_FILE $REPL_SCRIPT_FILE && \
    mkdir "$REPL_LIBS_PATH"

WORKDIR $CWD

# Set entrypoint (I couldn't figure out how to use the $CFG variable here)
ENTRYPOINT [ "/repl/entrypoint.sh" ]
#ENTRYPOINT [ "eval $REPL_ENTRYPOINT" ]
