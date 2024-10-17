# Specify compiler
CC=gcc

# Specify linker
LINK=gcc

# Define output directories
OBJ_DIR=obj
BIN_DIR=Debug

# Create output directories if they don't exist
$(shell mkdir -p $(OBJ_DIR) $(BIN_DIR))

# Build all target
.PHONY: all
all: $(BIN_DIR)/app.exe

# Link the object files and dependent libraries into a binary
$(BIN_DIR)/app.exe: $(OBJ_DIR)/hello.o
	$(LINK) -o $@ $< -lstdc++

# Compile the source files into object files
$(OBJ_DIR)/hello.o: hello.c
	$(CC) -c $< -o $@

# Clean target
.PHONY: clean
clean:
	rm -rf $(OBJ_DIR)/*.o $(BIN_DIR)/app.exe
