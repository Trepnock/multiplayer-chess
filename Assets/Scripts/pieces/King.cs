using UnityEngine;
using System.Collections.Generic;//needed for Lists

public class King : ChessPiece
{
    //need to check all combinations of -1, 0, and 1, in coords, add if not allied, difficulty is map edges
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        //add if not null
        for (int x = -1; x < 2; x++)
        {
            for (int y = -1; y < 2; y++)
            {
                if (CheckAdjacent(ref board, tileCountX, tileCountY, x, y) is Vector2Int valid)
                {
                    r.Add(valid);
                }
            }
        }


        return r;
    }

    private Vector2Int? CheckAdjacent(ref ChessPiece[,] board, int tileCountX, int tileCountY, int x, int y)//? allows returning null
    {

        //if( board?[currentX + x, currentY + y] is ChessPiece)
        if (currentX + x > -1 && currentY + y > -1 && currentX + x < tileCountX && currentY + y < tileCountY)
        {
            if (board[currentX + x, currentY + y]?.team != team)//? might not be needed
            {
                //UnityEngine.Debug.Log("x = " + x + " y = " + y + " currentX = " + currentX + " currentY = " + currentY);
                return new Vector2Int(currentX + x, currentY + y);
            }
        }
        return null;
    }
    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        SpecialMove r = SpecialMove.None;

        //just use my hasMoved bool for king and rooks
        if (hasMoved == false)
        {
            if (board[0, currentY].hasMoved == false && board[0, currentY].pieceType == ChessPieceType.Rook)//left, shouldn't need to confirm it's a rook
            {
                //could probably reuse the rooks move rule to check if it's clear
                if (GetAvailableMovesFlex(ref board, 8, 8, MoveType.Rows).Contains(new Vector2Int(1,currentY)) && board[1, currentY] == null)
                {
                    availableMoves.Add(new Vector2Int(currentX-2, currentY));
                    r = SpecialMove.Castling;
                }
            }
            if (board[7, currentY].hasMoved == false && board[7, currentY].pieceType == ChessPieceType.Rook)//right
            {
                if (GetAvailableMovesFlex(ref board, 8, 8, MoveType.Rows).Contains(new Vector2Int(6, currentY)) && board[6, currentY] == null)
                {
                    availableMoves.Add(new Vector2Int(currentX+2, currentY));
                    r = SpecialMove.Castling;
                }
            }
        }

        return r;
    }
}


