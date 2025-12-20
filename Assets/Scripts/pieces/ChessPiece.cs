using NUnit.Framework;
using System;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;//needed for Lists

public enum ChessPieceType
{
    None = 0,
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6
}

public enum MoveType
{
    None = 0,
    Pawn,
    Rows,
    Knight,
    Diagonals,
    Both,
    King
}

public class ChessPiece : MonoBehaviour
{
    public int team;
    public ChessPieceType pieceType;
    public int currentX;
    public int currentY;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;
    public bool hasMoved = false;

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 5);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }

    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;
    }
    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();
        r.Add(new Vector2Int(3, 3));//for testing gets overriden
        r.Add(new Vector2Int(4, 3));//for testing
        r.Add(new Vector2Int(4, 4));//for testing
        r.Add(new Vector2Int(3, 4));//for testing

        return r;
    }

    //for getting rows/diagonals
    protected List<Vector2Int> GetAvailableMovesFlex(ref ChessPiece[,] board, int tileCountX, int tileCountY, MoveType moveType)
    {
        List<Vector2Int> r2d2 = new List<Vector2Int>();
        if (moveType is MoveType.Rows or MoveType.Both)
        {
            //UnityEngine.Debug.Log("rows");
            for (int i = currentX + 1; i < tileCountX; i++)
            {
                //r2d2 add the vector from iterate?
                //UnityEngine.Debug.Log(i + " first for loop");
                if (board[i, currentY]?.team == team)
                    break;
                r2d2.Add(IterateOverBoardrow(ref board, i, false));
                if (board[r2d2.Last().x, r2d2.Last().y]?.team != team && board[r2d2.Last().x, r2d2.Last().y] != null)//is enemy and not empty
                    break;
            }

            for (int i = currentY + 1; i < tileCountY; i++)
            {
                //UnityEngine.Debug.Log(i + " second for loop");
                if (board[currentX, i]?.team == team)
                    break;
                r2d2.Add(IterateOverBoardrow(ref board, i, true));
                if (board[r2d2.Last().x, r2d2.Last().y]?.team != team && board[r2d2.Last().x, r2d2.Last().y] != null)//is enemy and not empty
                    break;
            }

            for (int i = currentX - 1; i > -1; i--)
            {
                //UnityEngine.Debug.Log(i + " third for loop");

                if (board[i, currentY]?.team == team)
                    break;
                r2d2.Add(IterateOverBoardrow(ref board, i, false));
                if (board[r2d2.Last().x, r2d2.Last().y]?.team != team && board[r2d2.Last().x, r2d2.Last().y] != null)//is enemy and not empty
                    break;
            }

            for (int i = currentY - 1; i > -1; i--)
            {
                //UnityEngine.Debug.Log(i + " fourth for loop");

                if (board[currentX, i]?.team == team)
                    break;
                r2d2.Add(IterateOverBoardrow(ref board, i, true));
                if (board[r2d2.Last().x, r2d2.Last().y]?.team != team && board[r2d2.Last().x, r2d2.Last().y] != null)//is enemy and not empty
                    break;
            }
        }

        if (moveType is MoveType.Diagonals or MoveType.Both)
        {
            //UnityEngine.Debug.Log("diagonals");

            //bishop is at 5,0, needs 4,1, 3,2, 2,3 1,4, 0,5, and 6,1, 7,2
            for (int i = 1; i < (tileCountX) - currentX; i++) // i<9-5, looks at 3 moves, should be 2
            {//both positive
                //UnityEngine.Debug.Log(i + " first for loop");
                if (tileCountY - 1 < i + currentY)//take black bishop, at 7, can't move up, should break at i==1
                    break;//if we hit y limit befor x

                if (board[currentX + i, currentY + i]?.team != team)
                {
                    r2d2.Add(IterateOverBoardDiagonal(ref board, i, false));
                    if (board[r2d2.Last().x, r2d2.Last().y]?.team != team && board[r2d2.Last().x, r2d2.Last().y] != null)//is enemy and not empty
                        break;
                }
                else
                {
                    break;//if it's a team piece break
                }
            }

            for (int i = -1; currentX + i > -1; i--) // what if we make it 4 x loops
            {//both negative
                //UnityEngine.Debug.Log(i + " second for loop");
                if (currentY + i < 0)//take black bishop, at 7, can't move up, should break at i==1
                    break;//if we hit y limit befor x

                if (board[currentX + i, currentY + i]?.team != team)
                {
                    r2d2.Add(IterateOverBoardDiagonal(ref board, i, false));
                    if (board[r2d2.Last().x, r2d2.Last().y]?.team != team && board[r2d2.Last().x, r2d2.Last().y] != null)//is enemy and not empty
                        break;
                }
                else
                {
                    break;//if it's a team piece break
                }
            }
            for (int i = -1; currentX + i > -1; i--) // what if we make it 4 x loops
            {//x- y+
                //UnityEngine.Debug.Log(i + " third for loop");
                if (tileCountY - 1 < currentY - i)//take black bishop, at 7, can't move up, should break at i==1
                    break;//if we hit y limit befor x

                if (board[currentX + i, currentY - i]?.team != team)
                {
                    r2d2.Add(IterateOverBoardDiagonal(ref board, i, true));
                    if (board[r2d2.Last().x, r2d2.Last().y]?.team != team && board[r2d2.Last().x, r2d2.Last().y] != null)//is enemy and not empty
                        break;
                }
                else
                {
                    break;//if it's a team piece break
                }
            }
            for (int i = 1; i < (tileCountX) - currentX; i++) // what if we make it 4 x loops
            {//x+ y-
                //UnityEngine.Debug.Log(i + " fourth for loop");
                if (currentY - i < 0)//take black bishop, at 7, can't move up, should break at i==1
                    break;//if we hit y limit befor x

                if (board[currentX + i, currentY - i]?.team != team)
                {
                    r2d2.Add(IterateOverBoardDiagonal(ref board, i, true));
                    if (board[r2d2.Last().x, r2d2.Last().y]?.team != team && board[r2d2.Last().x, r2d2.Last().y] != null)//is enemy and not empty
                        break;
                }
                else
                {
                    break;//if it's a team piece break
                }
            }
        }

        return r2d2;
    }

    //direction? need to determine if we are iterating over rank or over file, for side-side, and then
    private Vector2Int IterateOverBoardrow(ref ChessPiece[,] board, int i, bool axis) //protected makes it private to non-sub-classes
    {
        Vector2Int checking = new Vector2Int(-1, -1);
        //need to check if we are using current y or current x, case?
        if (axis == false)//horizontal
        {
            checking = new Vector2Int(i, currentY);
        }
        else
        {//vertical
            checking = new Vector2Int(currentX, i);
        }

        if (board[checking.x, checking.y] == null)
            return checking;
        if (board[checking.x, checking.y].team == team)
        {
            UnityEngine.Debug.Log("fuck1");
            return new Vector2Int(-1, -1);//should never happen
        }
        return checking;//should happen for enemy team
    }

    protected Vector2Int IterateOverBoardDiagonal(ref ChessPiece[,] board, int i, bool negativeY)//need to adjust how i is used for coords
    {
        Vector2Int checking = new Vector2Int(-1, -1);

        checking = new Vector2Int(currentX + i, negativeY ? currentY - i : currentY + i);//i goes from 1 to the length to a limit, but diagonal or normal


        if (board[checking.x, checking.y] == null)
            return checking;
        if (board[checking.x, checking.y].team == team)
        {
            UnityEngine.Debug.Log("fuck2");
            return new Vector2Int(-1, -1);//should never happen
        }
        return checking;//should happen for enemy team

    }

    public virtual SpecialMove GetSpecialMoves (ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        /*if (pieceType == ChessPieceType.Pawn)
        {
            int direction = (team == 0) ? 1 : -1;

            //en passant and promotion

            //en passant, check against movelist to see if last move was a double move from a pawn
            //moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });

            //was it a pawn that moved, did it move two, and can it be taken by this piece
            bool isPawn = board[moveList.Last.y].pieceType == ChessPieceType.Pawn;
            bool doubleMoved = Math.Abs(moveList.Last.x.y - moveList.Last.y.y) == 2;//did it move by 2 in y
            bool takeable = moveList.Last.y.y == currentY;       //1-3, takeable if black at 3, 6-4 takeable if white at 4
            bool enemy = board[moveList.Last.y].team != team;//how do I get target square..
            if (isPawn && doubleMoved && takeable && enemy )
            {
                //need to return move as valid and ensure if taken it also performs the kill operation on the other piece
            }
        }

        if (pieceType == ChessPieceType.King && hasMoved == false)
        {
            //castling
            //iterate over backrow of board, ensure the first piece hit is rook, ensure king and rook have hasMoved == false

            for (int i = 1; i + currentY < 8; i++) //return .none if can't be done, otherwise need to ensure rook also moves
            {
                if (board.
            }
        }*/

        return SpecialMove.None; 
    }
}
