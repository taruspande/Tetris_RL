using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class PlayTetris : Agent
{
    public Board board;
    private int actionsTakenThisStep = 0;
    private bool rotationPerformed = false;
    public override void Initialize()
    {
        base.Initialize();
        this.actionsTakenThisStep=0;
        this.rotationPerformed = false;
    }

    public override void OnEpisodeBegin()
    {
        this.board.ClearLines(); 
        this.board.SpawnPiece(); 
        this.rotationPerformed = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        int[,] boardState = this.board.GetBoardState();
        for (int row = 0; row < this.board.boardSize.y; row++)
        {
            for (int col = 0; col < this.board.boardSize.x; col++)
            {
                // 1 for filled cell, 0 for empty cell
                sensor.AddObservation(boardState[col, row]);
            }
        }

        // Add observations for the falling Tetromino
        Vector3Int tetrominoPosition = this.board.activePiece.position;
        TetrominoData tetrominoData = this.board.activePiece.data;
        for (int i = 0; i < tetrominoData.cells.Length; i++)
        {
            Vector3Int cell = new Vector3Int(tetrominoData.cells[i].x + tetrominoPosition.x, tetrominoData.cells[i].y + tetrominoPosition.y, 0);
            sensor.AddObservation(cell.x);
            sensor.AddObservation(cell.y);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers){
        ActionSegment<float> continuousActions = actionBuffers.ContinuousActions;
        ActionSegment<int> discreteActions = actionBuffers.DiscreteActions;

        // Convert the continuous actions into discrete actions
        int rotation = Mathf.Clamp(Mathf.FloorToInt(continuousActions[0]), -1, 1);
        int left = discreteActions[1];
        int right = discreteActions[2];
        //int drop = Mathf.Clamp(Mathf.FloorToInt(continuousActions[0]), -1, 1);
        // Perform the chosen action on the Tetris board
        if (left==1 & right==0){
            this.board.activePiece.MoveLeft();
            actionsTakenThisStep++;
        }
        else if (right==1 & left==0){
            this.board.activePiece.MoveRight();
            actionsTakenThisStep++;
        }

        if (rotation == -1){
            this.board.activePiece.RotateCounterClockwise();
            actionsTakenThisStep++;
        }
        else if (rotation == 1){
            this.board.activePiece.RotateClockwise();
            actionsTakenThisStep++;
        }

        if (discreteActions[0] == 0)
        {
            if (this.board.activePiece.MoveDown())
            {
                // If the piece can still move down, don't lock yet
                actionsTakenThisStep = 0;
            }
            else
            {
                // The piece cannot move down, so lock it in place
                this.board.activePiece.Lock();
                actionsTakenThisStep = 0;

                // Calculate the reward for the agent based on the game state after locking
                float reward = this.board.CalculateReward();
                int linesCleared = this.board.linesCleared;
                AddReward(reward);
                Debug.Log("Reward: " + reward + "  LinesCleared: " + linesCleared);
                // Spawn a new piece after locking
                if (this.board.IsGameOver())
                {
                    EndEpisode();
                }
                else
                {
                    // Spawn a new piece after locking if the game is not over
                    this.board.SpawnPiece();
                }
            }
        }
        rotationPerformed = false;
        // if (drop == 1){
        //     if (!this.board.IsGameOver())
        //     {
        //         this.board.activePiece.HardDrop();
        //         actionsTakenThisStep = 0;
        //     }
        // 
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        // Reset all actions to zero
        continuousActions[0] = 0.0f; // Rotation
        discreteActions[0] = 0; // Vertical movement (down)
        discreteActions[1] = 0; // Horizontal movement (left)
        discreteActions[2] = 0; // Horizontal movement (right)

        // Handle continuous actions (rotation)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            continuousActions[0] = -1.0f; // Counter-clockwise rotation
            rotationPerformed = true;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            continuousActions[0] = 1.0f; // Clockwise rotation
            rotationPerformed = true;
        }
        else
        {
            continuousActions[0] = 0.0f; // No rotation
            rotationPerformed = false;
        }

        // Handle discrete actions (move down, move left, move right)
        if (Input.GetKey(KeyCode.DownArrow))
        {
            discreteActions[0] = 1; // Move down
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            discreteActions[1] = 1; // Move left
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            discreteActions[2] = 1; // Move right
        }
        if(this.board.IsGameOver()){
            EndEpisode();
        }
    }

}