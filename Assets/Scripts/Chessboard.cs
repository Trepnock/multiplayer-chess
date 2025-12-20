using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;//needed for Lists
using System.Linq;


public enum SpecialMove
{
    None,
    EnPassant,
    Promotion,
    Castling
}

public class Chessboard : MonoBehaviour
{

    [Header("art")]
    [SerializeField]
    private Material tileMaterial;
    [SerializeField]
    private Material hoverMaterial;
    [SerializeField]
    private Material highlightMaterial;
    [SerializeField]
    private float tileSize = 1.0f;
    [SerializeField]
    private Vector3 boardCenter = Vector3.zero;
    [SerializeField]
    private GameObject victoryScreen;

    [Header("Prefabs and Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teammaterials;
    [SerializeField] private float deathSize = 0.5f;
    [SerializeField] private float deathSpacing = 0.5f;
    [SerializeField] private float dragOffset = 0.5f;

    //Logic
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private const int tileCount_x = 8;
    private const int tileCount_y = 8;
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();//historical move list
    private SpecialMove specialMove;


    private GameObject[,] tiles = new GameObject[tileCount_x, tileCount_y];
    //private GameObject[,] tiles; causes a null reference, need to make them before the generate all tiles I guess...
    //GameObject[] Tiles = new GameObject[Size];
    /*[SerializeField] 
    private Camera currentCamera;*/
    private Vector3 bounds;//not used
    private bool isWhiteTurn;
    private Camera currentCamera;

    private Vector2Int currentHover;

    private void Awake()
    {
        GenerateAllTiles(tileSize, tileCount_x, tileCount_y);
        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
        //UnityEngine.Debug.Log(hoverMaterial);//the serialized material is null on awake...
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }
        RaycastHit info;
        //Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition); //uses old input manager
        //InputSystem.actions.FindAction("Point");// is a vector 2 control type, so should pass in 2 vectors
        Ray ray = currentCamera.ScreenPointToRay(Mouse.current.position.ReadValue());//this isn't changing improperly
        /*move to switch case, 
         * case 1 hitting tile and current hover -1, 
         * case 2 hitting tile and it's not hitposition (which hitposition should never be tile), 
         * case 3 not hitting a tile or a hover
         * case 4 holding mouse
         * case 5 releasing mouse (not sure how the holding/releasing will work, may need to be ifs in the switch?
         */
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);
            //Debug.Log(currentHover + " " + hitPosition);
            //if we're hovering a tile after not hovering over one
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                /*Debug.Log("case 1");
                Debug.Log(ray);
                Debug.Log(Physics.Raycast(ray, out info, 10000, LayerMask.GetMask("Tile")));*/
            }

            //if we had a previous tile
            if (currentHover != hitPosition)
            {
                //revert the old to the tile layer then move on to marking the current tile
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                /*Debug.Log("case 2");
                Debug.Log(ray);*/
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    if (chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn || chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn)//turn
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                        //Debug.Log("dragging piece");

                        //get list of valid moves, then highlight
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, tileCount_x, tileCount_y);

                        //also get list of specials
                        specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);
                        //UnityEngine.Debug.Log(availableMoves[1]);
                        HighlightTiles();

                    }
                }
            }
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);


                bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                if (!validMove)
                {
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                }
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }
        else
        { //resets old tie if you go off the board
            if (currentHover != -Vector2Int.one && !Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Hover")))
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
                /*Debug.Log("case 3");
                Debug.Log(ray);
                Debug.Log(Physics.Raycast(ray, out info, 10000, LayerMask.GetMask("Tile")));*/
            }
            if (currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }
        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * 0.001f);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
        }

        //reset materials because idek anymore, do I need to iterate over tiles?
        for (int x = 0; x < tileCount_x; x++)
        {
            for (int y = 0; y < tileCount_y; y++)
            {
                switch (tiles[x, y].layer) //tile 3, hover 6, highlight 7
                {
                    case 3:
                        tiles[x, y].GetComponent<MeshRenderer>().material = tileMaterial;
                        //UnityEngine.Debug.Log("case 3");
                        break;
                    case 6:
                        tiles[x, y].GetComponent<MeshRenderer>().material = hoverMaterial;
                        //UnityEngine.Debug.Log("case 6  " + hoverMaterial);
                        break;
                    case 7:
                        tiles[x, y].GetComponent<MeshRenderer>().material = highlightMaterial;
                        //UnityEngine.Debug.Log("case 7");
                        break;
                    default:
                        break;
                }
            }
        }

    }


    //make the board
    private void GenerateAllTiles(float tileSize, int tilecountx, int tilecounty)
    {
        //yOffset += transform.position.y;
        bounds = new Vector3((tilecountx / 2) * tileSize, 0, (tilecounty / 2) * tileSize) + boardCenter;
        for (int x = 0; x < tilecountx; x++)
        {
            for (int y = 0; y < tilecounty; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
            }
        }
    }
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, 0, y * tileSize);
        vertices[1] = new Vector3(x * tileSize, 0, (y + 1) * tileSize);
        vertices[2] = new Vector3((x + 1) * tileSize, 0, y * tileSize);
        vertices[3] = new Vector3((x + 1) * tileSize, 0, (y + 1) * tileSize);

        int[] triangles = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();


        return tileObject;
        // why do it this way instead of having a tile prefab? even a board prefab? maybe makes it easier to interact with later.
    }

    //set up pieces
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[tileCount_x, tileCount_y];

        int whiteTeam = 0;
        int blackTeam = 1;

        //white team
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);//white knights need rotated
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        for (int i = 0; i < tileCount_x; i++)
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);

        //black team
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        for (int i = 0; i < tileCount_x; i++)
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        GameObject pieceObject = Instantiate(prefabs[(int)type], transform);
        ChessPiece p = pieceObject.GetComponent<ChessPiece>();
        p.pieceType = type;
        p.team = team;
        pieceObject.GetComponentInChildren<MeshRenderer>().material = teammaterials[team];
        if (type == ChessPieceType.Knight && team == 0)
        {
            pieceObject.transform.Rotate(0, 180, 0);
        }

        return p;
    }

    //Positioning
    private void PositionAllPieces()
    {
        for (int x = 0; x < tileCount_x; x++)
        {
            for (int y = 0; y < tileCount_y; y++)
            {
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
            }
        }
    }

    private void PositionSinglePiece(int x, int y, bool force = false) //force is instant vs smooth movement
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        //chessPieces[x, y].transform.position = new Vector3((0.5f+x) * tileSize, 0, (0.5f+y) * tileSize);
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);

    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x + tileSize, 0, y + tileSize) - new Vector3(1, 0, 1) + new Vector3(tileSize / 2, 0, tileSize / 2);
    }


    //operations
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        //get index of tile moused over
        for (int x = 0; x < tileCount_x; x++)
        {
            for (int y = 0; y < tileCount_y; y++)
            {
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);
            }
        }
        return -Vector2Int.one; //returns -1/-1 if you aren't hitting a board square
    }

    private bool MoveTo(ChessPiece piece, int x, int y)
    {
        if (ContainsValidMove(ref availableMoves, new Vector2(x, y)) == false)//this should be inverted but uh, it works?
        {
            //bool testbool = false;
            //UnityEngine.Debug.Log(testbool);
            //UnityEngine.Debug.Log("it's this damn thing" + ContainsValidMove(ref availableMoves, new Vector2(x, y)));
            return false;
        }

        Vector2Int previousPosition = new Vector2Int(piece.currentX, piece.currentY);
        //UnityEngine.Debug.Log("got past the contains check in moveto");
        //Is there another piece at destination
        if (chessPieces[x, y] != null)
        {
            //UnityEngine.Debug.Log("first if");

            ChessPiece otherPiece = chessPieces[x, y];

            if (piece.team == otherPiece.team)
            {
                //UnityEngine.Debug.Log("second if");
                return false;//can't take your own pieces
            }
            else//put taken piece on side of board
            {
                killPiece(otherPiece, piece);
            }


        }
        //UnityEngine.Debug.Log("past the ifs");
        chessPieces[x, y] = piece;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);
        isWhiteTurn = !isWhiteTurn;
        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });
        piece.hasMoved = true;
        ProcessSpecialMove();
        return true;
    }

    private void HighlightTiles()
    {

        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight"); //the render method to change material is being a pain in the ass
        }
    }

    private void RemoveHighlightTiles()//might be able to bake into the above
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        }
        availableMoves.Clear();
    }

    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        //why is this getting called without a piece dragged -- because it's used to re-highlight when you hover a highlight
        //UnityEngine.Debug.Log("ContainsValidMove called");
        //availableMoves is moves, being called in the MoveTo function, Vector 2 is the x and y of the hovered tile,
        for (int i = 0; i < moves.Count; i++)
        {
            //UnityEngine.Debug.Log("in the ContaintValidMove for loop i=" + i);
            if (moves[i].x == pos.x && moves[i].y == pos.y)
            {
                //UnityEngine.Debug.Log("true");
                return true;
            }
        }
        //UnityEngine.Debug.Log("false");
        return false;
    }

    private void CheckMate(int team)
    {
        DisplayVictory(team);
    }

    private void DisplayVictory(int team)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(team).gameObject.SetActive(true);
    }

    public void OnResetButton()
    {
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();

        //nuke pieces
        foreach (ChessPiece i in chessPieces)//really doesn't want to let me use where on chessPieces
        {
            if (i != null)
            {
                int y = i.currentY;
                int x = i.currentX;
                Destroy(i.gameObject);
                chessPieces[x, y] = null;
            }//this might run into issues with currentx/y getting destroyed
        }

        foreach (ChessPiece i in deadBlacks)
        {
            Destroy(i.gameObject);
        }
        foreach (ChessPiece i in deadWhites)
        {
            Destroy(i.gameObject);
        }

        deadBlacks.Clear();
        deadWhites.Clear();

        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }
    public void OnExitButton()
    {
        Application.Quit();
    }

    private void killPiece(ChessPiece otherPiece, ChessPiece piece)
    {
        if (otherPiece.pieceType == ChessPieceType.King)
        {
            CheckMate(piece.team);
        }
        //UnityEngine.Debug.Log("first else");
        if (otherPiece.team == 0)
        {
            deadWhites.Add(otherPiece);
            otherPiece.SetScale(Vector3.one * deathSize);
            otherPiece.SetPosition(new Vector3(8 * tileSize, 0, -1 * tileSize) + new Vector3(tileSize * .5f, 0, tileSize * .5f) + (Vector3.forward * deathSpacing) * (deadWhites.Count));
        }
        else
        {
            deadBlacks.Add(otherPiece);
            otherPiece.SetScale(Vector3.one * deathSize);
            otherPiece.SetPosition(new Vector3(-1 * tileSize, 0, 8 * tileSize) + new Vector3(tileSize * .5f, 0, tileSize * .5f) + (Vector3.forward * deathSpacing) * -(1 + deadBlacks.Count));
        }
    }

    //special moves
    private void ProcessSpecialMove()
    {
        if (specialMove == SpecialMove.EnPassant)
        {
            var newMove = moveList[moveList.Count - 1];
            var targetPawnPosition = moveList[moveList.Count - 2];
            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            if (myPawn.currentX == enemyPawn.currentX)
            {
                killPiece(enemyPawn, myPawn);
            }
            chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
        }

        if (specialMove == SpecialMove.Castling)
        {
            var lastMove = moveList[moveList.Count - 1];
            ChessPiece king = chessPieces[lastMove[1].x, lastMove[1].y];

            if (Mathf.Abs(lastMove[1].x - lastMove[0].x) ==2)//king/rook   6,5 2,3  
            {
                ChessPiece rook = chessPieces[7, king.currentY];
                switch (king.currentX)
                { 
                case 6: 
                        
                    rook = chessPieces[7, king.currentY];
                    chessPieces[5,king.currentY] = rook;
                    PositionSinglePiece(5, king.currentY);
                    chessPieces[7, king.currentY] = null;
                    break;

                case 2: 
                    rook = chessPieces[0, king.currentY];
                    chessPieces[3, king.currentY] = rook;
                    PositionSinglePiece(3, king.currentY);
                    chessPieces[0, king.currentY] = null;
                    break;
                /*case 1:
                    rook = chessPieces[0, king.currentY];
                    chessPieces[2, king.currentY] = rook;
                    PositionSinglePiece(3, king.currentY);
                    chessPieces[0, king.currentY] = null;
                    break;
                case :
                    rook = chessPieces[0, king.currentY];
                    chessPieces[2, king.currentY] = rook;
                    PositionSinglePiece(3, king.currentY);
                    chessPieces[0, king.currentY] = null;
                    break;*/
                default:
                    break;
                }
            }
        }

        if(specialMove == SpecialMove.Promotion)
        {
            var lastMove = moveList[moveList.Count - 1];
            ChessPiece pawn = chessPieces[lastMove[1].x, lastMove[1].y];
            if(pawn.pieceType == ChessPieceType.Pawn)
            {
                if (pawn.currentY == 0 || pawn.currentY == 7)
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, pawn.team);
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y, true);
                }
            }
        }
    }
}
