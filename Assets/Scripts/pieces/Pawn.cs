using UnityEngine;
using System.Collections.Generic;//needed for Lists
public class Pawn : ChessPiece
{
    
    //use update to check for promotion?
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        //check if next space is occupied, if not can move forward, might run into issues when it gets to promotion range but, eh
        bool nextSpaceOpen = board[currentX, currentY + direction] == null;
        bool notOffEdge = currentY + direction * 2 >0 && currentY + direction * 2 < tileCountY;

        if (nextSpaceOpen)
        {
            r.Add(new Vector2Int(currentX, currentY + direction));

            if (notOffEdge)
            {
                bool secondSpaceOpen = board[currentX, currentY + 2 * direction] == null;
                if (hasMoved == false && secondSpaceOpen)
                    if (StartMove(ref board, direction) is Vector2Int valid)
                        r.Add(valid);
            }
        }
        //double start move is above

        //taking
        if (currentX != tileCountX - 1)//is space not empty and not friendly... should be able to drop not friendly but default team is probably 0 so could have some issues there
            if (board[currentX + 1, currentY + direction] != null && board[currentX + 1, currentY + direction].team != team)
{
                //UnityEngine.Debug.Log(currentX + 1 + " " + currentY + direction);

                r.Add(new Vector2Int(currentX + 1, currentY + direction));
}        if (currentX != 0)//is space not empty and not friendly... should be able to drop not friendly but default team is probably 0 so could have some issues there
            if (board[currentX - 1, currentY + direction] != null && board[currentX - 1, currentY + direction].team != team)
{
                //UnityEngine.Debug.Log(currentX - 1 + " " + currentY + direction);
                r.Add(new Vector2Int(currentX - 1, currentY + direction));
}

        return r;
    }

    private Vector2Int? StartMove(ref ChessPiece[,] board, int direction)
    {
        switch (team)
        {
            case 0 when currentY == 1:
                return (new Vector2Int(currentX, currentY + direction * 2));
            case 1 when currentY == 6:
                return (new Vector2Int(currentX, currentY + direction * 2));
            default:
                return null;
        }


    }

    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        int direction = (team == 0) ? 1 : -1;

        //en passant
        if (moveList.Count > 0)
        { 
            Vector2Int[] lastmove = moveList[moveList.Count - 1];
            if (board[lastmove[1].x, lastmove[1].y].pieceType == ChessPieceType.Pawn)
            {
                if (Mathf.Abs(lastmove[0].y - lastmove[1].y) == 2)
                {
                    //if (board[lastmove[1].x, lastmove[1].y].team != team//shouldn't this always be true with alternating turns?
                    //{
                        if (lastmove[1].y == currentY)
                        {
                            if (Mathf.Abs(currentX - lastmove[1].x) == 1)
                            {
                                availableMoves.Add(new Vector2Int(lastmove[1].x, currentY + direction));
                                return SpecialMove.EnPassant;
                            }
                        }

                    //}
                }
            }
        }

        if (currentY+direction == 0 || currentY+direction == 7)
        {
            return SpecialMove.Promotion;//really we can just check if a pawn is on the backline?
        }
        
        return SpecialMove.None; 
    }

}
