using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public class TicTacToeMinimax
{
    public static void Main(string[] args)
    {
        // Initial board state setup (x's turn)
        char[][] data = new char[3][]
        {
            new char[] {'x', 'x', ' '},
            new char[] {'o', 'o', ' '},
            new char[] {' ', ' ', ' '}
        };

        Console.WriteLine("Initial Board State:");
        PrintBoard(data);
        Console.WriteLine($"It is '{(IsMaxPlayerTurn(data) ? 'x' : 'o')}'s turn.");

        Stopwatch sw = Stopwatch.StartNew();
        // Calculate the optimal value of the initial state
        int optimalValue = Minimax(data, IsMaxPlayerTurn(data), int.MinValue, int.MaxValue);
        sw.Stop();

        Console.WriteLine("\n--- Alpha-Beta Minimax Evaluation ---");
        Console.WriteLine($"Optimal Board Value: {optimalValue}");
        Console.WriteLine($"Execution Time: {sw.Elapsed.TotalMilliseconds:F3} ms");
        Console.WriteLine("\nValue 1 = 'x' wins, -1 = 'o' wins, 0 = draw.");

        // Find and show the best move for the current player
        Console.WriteLine("\n--- Best Move Analysis ---");
        var (bestRow, bestCol) = FindBestMove(data);
        Console.WriteLine($"Best move: Row {bestRow}, Column {bestCol}");
    }

    // A class representing a node in the game state tree. 
    // While the recursive Minimax implementation below doesn't explicitly build this tree, 
    // it defines the structure if one were to be constructed.
    public class Tree
    {
        public char[][] Data { get; set; }
        public bool IsMaxPlayerTurn { get; set; }
        public int Value { get; set; }
        public List<Tree> Successors { get; set; } = new List<Tree>();
    }

    /*
     * Finds all empty (' ') places on the board and stores their (row, col) coordinates 
     * in the provided Span<int>.
     * * Optimization: Uses Span<int> and MethodImplOptions.AggressiveInlining 
     * to reduce heap allocations and function call overhead.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetEmptyPlaces(char[][] board, Span<int> coords)
    {
        int count = 0;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i][j] == ' ')
                {
                    // Store the row (i) and then the column (j)
                    coords[count++] = i;
                    coords[count++] = j;
                }
            }
        }
        // Returns the number of coordinate values written (2 * number of empty cells)
        return count; 
    }

    /*
     * Checks the board for a win condition (3 in a row, column, or diagonal).
     * * Optimization: Checks diagonals and the center cell first for early termination, 
     * as this covers the most frequently checked win lines.
     * * Returns: 
     * 1 if 'x' wins (Max Player)
     * -1 if 'o' wins (Min Player)
     * 0 if no one has won yet or it's a draw
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WhoWin(char[][] board)
    {
        char center = board[1][1];
        if (center != ' ')
        {
            // Main diagonal
            if (board[0][0] == center && board[2][2] == center)
                return center == 'x' ? 1 : -1;
            
            // Anti-diagonal
            if (board[0][2] == center && board[2][0] == center)
                return center == 'x' ? 1 : -1;
        }

        // Check rows and columns
        for (int i = 0; i < 3; i++)
        {
            // Row check: horizontal win
            char firstRow = board[i][0];
            if (firstRow != ' ' && board[i][1] == firstRow && board[i][2] == firstRow)
                return firstRow == 'x' ? 1 : -1;
            
            // Column check: vertical win
            char firstCol = board[0][i];
            if (firstCol != ' ' && board[1][i] == firstCol && board[2][i] == firstCol)
                return firstCol == 'x' ? 1 : -1;
        }
        
        return 0; // No winner
    }

    // Determines whose turn it is based on the piece counts.
    // 'x' (Max Player) always goes first.
    // If countX == countO, it's 'x's turn. If countX > countO, it's 'o's turn.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMaxPlayerTurn(char[][] board)
    {
        int countX = 0;
        int countO = 0;
        
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                char cell = board[i][j];
                if (cell == 'x') countX++;
                else if (cell == 'o') countO++;
            }
        }
        
        return countX <= countO;
    }

    // Places a player's marker ('x' or 'o') on the board at (row, col).
    // This is an in-place modification.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ApplyMove(char[][] board, int row, int col, char player)
    {
        board[row][col] = player;
    }

    // Reverts a move by setting the cell back to empty (' ').
    // This is essential for backing up during the recursive Minimax search.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UndoMove(char[][] board, int row, int col)
    {
        board[row][col] = ' ';
    }

    // Utility function to print the Tic-Tac-Toe board to the console.
    public static void PrintBoard(char[][] board)
    {
        Console.WriteLine("---------");
        for (int i = 0; i < 3; i++)
        {
            Console.WriteLine($" {board[i][0]} | {board[i][1]} | {board[i][2]} ");
            if (i < 2)
                Console.WriteLine("---------");
        }
        Console.WriteLine("---------");
    }

    /*
     * The core recursive function to determine the optimal value of the current board state.
     * It uses the Minimax principle: Max player seeks to maximize the score (1), 
     * and Min player seeks to minimize the score (-1).
     * * Key Optimization: Alpha-Beta Pruning.
     */
    public static int Minimax(char[][] board, bool isMaxPlayerTurn, int alpha, int beta)
    {
        // Base case: Check if the game is over (win/loss).
        int score = WhoWin(board);
        if (score != 0)
            return score;

        // Get all available moves. Uses stack-allocated Span<int> for speed.
        Span<int> emptyCoords = stackalloc int[18]; 
        int coordCount = GetEmptyPlaces(board, emptyCoords);
        
        // Base case: Check if the board is full (draw).
        if (coordCount == 0)
            return 0; // Draw

        char currentPlayer = isMaxPlayerTurn ? 'x' : 'o';

        if (isMaxPlayerTurn) // Max Player ('x') wants to maximize the score
        {
            int maxEval = int.MinValue;
            
            for (int i = 0; i < coordCount; i += 2)
            {
                int row = emptyCoords[i];
                int col = emptyCoords[i + 1];
                
                // 1. Apply the move to the board (in-place)
                ApplyMove(board, row, col, currentPlayer);
                // 2. Recursively call Minimax for the opponent (Min Player)
                int eval = Minimax(board, false, alpha, beta);
                // 3. Undo the move to restore the board for the next sibling move
                UndoMove(board, row, col);
                
                // Update the best value found for the Max player
                if (eval > maxEval)
                    maxEval = eval;
                
                // Update Alpha: the best score Max can currently guarantee
                if (eval > alpha)
                    alpha = eval;
                
                // Beta Cutoff (Pruning): 
                // If Max's current best (alpha) is already >= Min's best guarantee (beta), 
                // Min will never choose this path, so we stop searching it.
                if (beta <= alpha)
                    break; 
            }
            
            return maxEval;
        }
        else // Min Player ('o') wants to minimize the score
        {
            int minEval = int.MaxValue;
            
            for (int i = 0; i < coordCount; i += 2)
            {
                int row = emptyCoords[i];
                int col = emptyCoords[i + 1];
                
                // 1. Apply the move
                ApplyMove(board, row, col, currentPlayer);
                // 2. Recursively call Minimax for the opponent (Max Player)
                int eval = Minimax(board, true, alpha, beta);
                // 3. Undo the move
                UndoMove(board, row, col);
                
                // Update the best value found for the Min player
                if (eval < minEval)
                    minEval = eval;
                
                // Update Beta: the best score Min can currently guarantee
                if (eval < beta)
                    beta = eval;
                
                // Alpha Cutoff (Pruning):
                // If Min's current best (beta) is already <= Max's best guarantee (alpha),
                // Max will never choose this path, so we stop searching it.
                if (beta <= alpha)
                    break; 
            }
            
            return minEval;
        }
    }

    // === Find Best Move Utility ===

    // Iterates through all possible moves and uses Minimax to find the highest/lowest scoring move.
    public static (int row, int col) FindBestMove(char[][] board)
    {
        bool isMax = IsMaxPlayerTurn(board);
        int bestValue = isMax ? int.MinValue : int.MaxValue;
        int bestRow = -1, bestCol = -1;

        // Get empty coordinates using stack allocation
        Span<int> emptyCoords = stackalloc int[18];
        int coordCount = GetEmptyPlaces(board, emptyCoords);

        char currentPlayer = isMax ? 'x' : 'o';

        for (int i = 0; i < coordCount; i += 2)
        {
            int row = emptyCoords[i];
            int col = emptyCoords[i + 1];
            
            // 1. Try the move
            ApplyMove(board, row, col, currentPlayer);
            // 2. Evaluate the move: call Minimax, switching to the other player. 
            // Start with full alpha/beta bounds for this top-level evaluation.
            int moveValue = Minimax(board, !isMax, int.MinValue, int.MaxValue);
            // 3. Undo the move
            UndoMove(board, row, col);

            // Update the best move found so far
            if (isMax && moveValue > bestValue)
            {
                bestValue = moveValue;
                bestRow = row;
                bestCol = col;
            }
            else if (!isMax && moveValue < bestValue)
            {
                bestValue = moveValue;
                bestRow = row;
                bestCol = col;
            }
        }

        return (bestRow, bestCol);
    }
}