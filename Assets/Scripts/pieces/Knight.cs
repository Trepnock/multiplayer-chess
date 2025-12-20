using UnityEngine;
using System.Collections.Generic;//needed for Lists



public class NewMonoBehaviourScript : ChessPiece
{
    private List<int> CoordOptions = new List<int> { -2, 2, -1, 1};
    //need to move 2 in every direction, then one in both perpindicular
    // -2,-1  -2,1  1,2, -1,2  2,1, 2,-1, 1,-2, -1,-2
    // -3, -1, 1, 3
    //must match a 1 with a 2
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();
        List<Vector2Int> moves = new List<Vector2Int>();
        moves = potentialMoves();
        //UnityEngine.Debug.Log(r);
        foreach (Vector2Int i in  moves)
        {
            if (currentX + i.x > -1 && currentY + i.y > -1 && currentX + i.x < tileCountX && currentY + i.y < tileCountY)
            {
                if (board[currentX + i.x, currentY + i.y]?.team != team)
                {
                    r.Add(new Vector2Int (currentX+i.x, currentY+i.y));
                }
            }
        
                  
        }

        return r;
    }

    // generate list of vectors to be added/subtracted from current
    private List<Vector2Int> potentialMoves()
    {
        List<Vector2Int> r = new List<Vector2Int>();

        for (int i = 0; i<2; i++)
        {
            for (int j = 2; j < 4; j++)
            {
                r.Add(new Vector2Int(CoordOptions[i], CoordOptions[j]));
                r.Add(new Vector2Int(CoordOptions[j], CoordOptions[i]));

            }
        }

        return r;
    }
}

