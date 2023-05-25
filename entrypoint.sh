#!/bin/bash

# Add all dlls found in $REPL_LIBS_PATH as references
include_libs() {
    for v in $(find $REPL_LIBS_PATH -mindepth 2 -maxdepth 2 -type f -iname *.dll); do
        echo "-r $v" >> "$REPL_RSP_FILE"
    done
}

ARGUMENTS=()

for var in "$@"; do
    if [[ "$var" == "--verbose" || "$var" == "--verbose=1" ]]; then
        VERBOSE=1
    elif [[ "$var" == "--verbose=2" ]]; then
        VERBOSE=2
    else
        ARGUMENTS+=($var)
    fi
done

[ $VERBOSE ] && echo "Arguments: ${ARGUMENTS[*]}"

include_libs

# Processing arguments
VALID_ARGS=$(getopt -o f:u: --long --framework:,using: -- ${ARGUMENTS[*]})

if [[ $? -ne 0 ]]; then
    exit 1;
fi

eval set -- "$VALID_ARGS"

while [ : ]; do
  case "$1" in -u | --using)
        [ $VERBOSE ] && echo "Adding using statement: $2"
        echo "-u $2" >> "$REPL_RSP_FILE"
        shift 2
        ;;
    -f | --framework)
        [ $VERBOSE ] && echo "Setting framework to $2"
        OPTION_FRAMEWORK="$2"
        shift 2
        ;;
    --) shift;
        break
        ;;
  esac
done

[ $VERBOSE ] && echo "Framework found in option: $OPTION_FRAMEWORK"
[ $VERBOSE ] && echo "Framework found in ENV variable: $REPL_FRAMEWORK"

FILE_FRAMEWORK=$(grep "^-f \|^--framework " "$REPL_RSP_FILE"  | sed 's/^-f \|^--framework //')
REPL_FRAMEWORK=${OPTION_FRAMEWORK:-${REPL_FRAMEWORK:-${FILE_FRAMEWORK:-Microsoft.NETCore.App}}}

[ $VERBOSE ] && echo "Framework found in response file: $FILE_FRAMEWORK"
[ $VERBOSE ] && echo "Chosen framework: $REPL_FRAMEWORK"

# Ensure correct framework present in rsp file
sed -i '/^-f \|^--framework /d' "$REPL_RSP_FILE"
echo "-f $REPL_FRAMEWORK" >> "$REPL_RSP_FILE"

[ $VERBOSE ] && echo "Running preprocessing of response file..."
# Transform usings
if [[ $VERBOSE == 2 ]]; then
    eval "$REPL_RSP_PREPROCESSING_CMD"
else
    eval "$REPL_RSP_PREPROCESSING_CMD" 2>&1 > /dev/null
fi

[ $VERBOSE ] && echo "Running preprocessing of response file DONE!"
[ $VERBOSE ] && echo "Response file:" && cat "$REPL_RSP_FILE" && echo ""

exec /root/.dotnet/tools/csharprepl "@$REPL_RSP_FILE" "$REPL_SCRIPT_FILE" "$@"
