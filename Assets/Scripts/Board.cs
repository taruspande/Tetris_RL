using UnityEngine;
using UnityEngine.Tilemaps;

public class Board : MonoBehaviour
{
    public Tilemap tilemap {get; private set;}
    public Piece activePiece {get; private set;}
    public TetrominoData[] tetrominoes;
    public Vector3Int spawnPosition;
    public Vector2Int boardSize=new Vector2Int(10,20);
    public RectInt Bounds{
        get{
            Vector2Int position=new Vector2Int(-this.boardSize.x/2, -this.boardSize.y/2);
            return new RectInt(position, this.boardSize);
        }
    } 

    private void Awake(){
        this.tilemap=GetComponentInChildren<Tilemap>();
        this.activePiece=GetComponentInChildren<Piece>();

        for(int i=0; i<this.tetrominoes.Length; i++){
            this.tetrominoes[i].Initialize();
        }
    }

    private void Start(){
        SpawnPiece();
    }

    public void SpawnPiece(){
        int random=Random.Range(0, this.tetrominoes.Length);
        TetrominoData data=this.tetrominoes[random];

        this.activePiece.Initialize(this, this.spawnPosition, data);
        if(IsValidPosition(this.activePiece, this.spawnPosition)){
            Set(this.activePiece);
        }
        else{
            GameOver();
        }
    }

    private void GameOver(){
        this.tilemap.ClearAllTiles();
        Debug.Log("Game Over!!!");
    }

    public bool IsGameOver(){
    RectInt bounds = this.Bounds;
    for (int col = bounds.xMin; col < bounds.xMax; col++){
        Vector3Int position = new Vector3Int(col, bounds.yMax - 1, 0);
        if (this.tilemap.HasTile(position)){
            return true;
        }
    }
    return false;
}

    public void Set(Piece piece){
        for(int i=0; i<piece.cells.Length; i++){
            Vector3Int tilePosition=piece.cells[i]+piece.position;
            this.tilemap.SetTile(tilePosition, piece.data.tile);
        }
    }

    public void Clear(Piece piece){
        for(int i=0; i<piece.cells.Length; i++){
            Vector3Int tilePosition=piece.cells[i]+piece.position;
            this.tilemap.SetTile(tilePosition, null);
        }
    }

    public bool IsValidPosition(Piece piece, Vector3Int position){
        RectInt bounds=this.Bounds;

        for(int i=0; i<piece.cells.Length; i++){
            Vector3Int tilePosition=piece.cells[i]+position;
            if(!bounds.Contains((Vector2Int)tilePosition)){
                return false;
            }
            if(this.tilemap.HasTile(tilePosition)){
                return false;
            }
        }
        return true;
    }

    public void ClearLines(){
        RectInt bounds=this.Bounds;
        int row=bounds.yMin;
        while(row<bounds.yMax){
            if(IsLineFull(row)){
                LineClear(row);
            }
            else{
                row++;
            }
        }
    }

    private bool IsLineFull(int row){
        RectInt bounds=this.Bounds;
        for(int col=bounds.xMin; col<bounds.xMax; col++){
            Vector3Int position=new Vector3Int(col, row, 0);
            if(!this.tilemap.HasTile(position)){
                return false;
            }
        }
        return true;
    }

    private void LineClear(int row){
        RectInt bounds=this.Bounds;
        for(int col=bounds.xMin; col<bounds.xMax; col++){
            Vector3Int position=new Vector3Int(col, row, 0);
            this.tilemap.SetTile(position, null);
        }

        while(row<bounds.yMax){
            for(int col=bounds.xMin; col<bounds.xMax; col++){
                Vector3Int position=new Vector3Int(col, row+1, 0);
                TileBase above=this.tilemap.GetTile(position);
                position=new Vector3Int(col, row, 0);
                this.tilemap.SetTile(position, above);
            }
            row++;
        }
    }

    public int LinesCleared(){
        RectInt bounds = this.Bounds;
        int linesCleared=0;
        for (int row = bounds.yMin; row < bounds.yMax; row++){
            if(IsLineFull(row)){
                linesCleared++;
            }
        }
        return linesCleared;
    }

    public bool IsLineCleared(){
        int linesCleared=this.LinesCleared();
        return linesCleared == this.boardSize.y;
    }

    public int[,] GetBoardState(){
        int[,] boardState=new int[this.boardSize.x, this.boardSize.y];
        for (int row = 0; row < this.boardSize.y; row++){
            for (int col = 0; col < this.boardSize.x; col++){
                Vector3Int position=new Vector3Int(col, row, 0);
                if(this.tilemap.HasTile(position)){
                    boardState[col, row]=1;
                }
                else{
                    boardState[col, row]=0;
                }
            }
        }
        return boardState;
    }

    public float CalculateReward()
    {
        int linesCleared = this.LinesCleared();
        int maxHeight = CalculateMaxHeight();
        int holesCount = CountHoles();
        float reward = 0f;
        int left = this.activePiece.left;
        int right = this.activePiece.right;
        int steps = this.activePiece.steps;
        

        reward+=linesCleared;

        // reward+=(float)0.002*steps;

        reward-=(float)maxHeight/100;

        reward-=(float)holesCount/10;

        // reward-=Mathf.Abs((left-right)/1000);

        if(IsGameOver()){
             reward-=1f;
        }

        return reward;
    }

    private int CalculateMaxHeight()
{
    RectInt bounds = this.Bounds;
    int maxHeight = 0;

    for (int col = bounds.xMin; col < bounds.xMax; col++)
    {
        for (int row = bounds.yMin; row < bounds.yMax; row++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);
            if (this.tilemap.HasTile(position))
            {
                maxHeight = Mathf.Max(maxHeight, row - bounds.yMin);
                break; // Break to the next column once we find the highest filled cell in this column
            }
        }
    }

    return maxHeight;
}


    private int CountHoles()
    {
        RectInt bounds = this.Bounds;
        int holesCount = 0;

        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            for (int row = bounds.yMin; row < bounds.yMax; row++)
            {
                Vector3Int position = new Vector3Int(col, row, 0);

                if (!this.tilemap.HasTile(position))
                {
                    // Check if the cell is empty and surrounded by filled cells on all four sides
                    bool surroundedByFilledCells;
                    if(col==bounds.xMin){
                        surroundedByFilledCells =
                            this.tilemap.HasTile(position + Vector3Int.up) &&
                            this.tilemap.HasTile(position + Vector3Int.down) &&
                            this.tilemap.HasTile(position + Vector3Int.right);
                    }
                    else if(col==bounds.xMax){
                        surroundedByFilledCells =
                            this.tilemap.HasTile(position + Vector3Int.up) &&
                            this.tilemap.HasTile(position + Vector3Int.down) &&
                            this.tilemap.HasTile(position + Vector3Int.left);
                    }
                    else{
                        surroundedByFilledCells =
                            this.tilemap.HasTile(position + Vector3Int.up) &&
                            this.tilemap.HasTile(position + Vector3Int.down) &&
                            this.tilemap.HasTile(position + Vector3Int.left) &&
                            this.tilemap.HasTile(position + Vector3Int.right);
                    }

                    if (surroundedByFilledCells)
                    {
                        holesCount++; // Found a hole
                    }
                }
            }
        }

        return holesCount;
    }



}
