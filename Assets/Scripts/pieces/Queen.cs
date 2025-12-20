using UnityEngine;
using System.Collections.Generic;//needed for Lists

public class Queen : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        r = GetAvailableMovesFlex(ref board, tileCountX, tileCountY, MoveType.Both);

        return r;
    }
}
