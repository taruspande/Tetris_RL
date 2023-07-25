using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class PlayTetris : Agent
{
    public Board board;
    private int actionsTakenThisStep = 0;

    public override void Initialize()
    {
        base.Initialize();
        this.actionsTakenThisStep=0;
    }

    public override void OnEpisodeBegin()
    {
        this.board.ClearLines(); 
        this.board.SpawnPiece(); 
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
        int moveDirection = discreteActions[0];
        int rotation = discreteActions[1];
        int drop = Mathf.Clamp(Mathf.FloorToInt(continuousActions[0]), -1, 1);
        // Perform the chosen action on the Tetris board
        if(actionsTakenThisStep<3){
            if (moveDirection == 0){
                this.board.activePiece.MoveLeft();
                actionsTakenThisStep++;
            }
            else if (moveDirection == 1){
                this.board.activePiece.MoveRight();
                actionsTakenThisStep++;
            }

            if (rotation == 0){
                this.board.activePiece.RotateCounterClockwise();
                actionsTakenThisStep++;
            }
            else if (rotation == 1){
                this.board.activePiece.RotateClockwise();
                actionsTakenThisStep++;
            }
        }

        if (drop == 1){
            this.board.activePiece.HardDrop();
            actionsTakenThisStep=0;
        }

        else if(drop == -1){
            this.board.activePiece.MoveDown();
            actionsTakenThisStep=0;
        }

        // Calculate the reward for the agent based on the game state
        float reward = this.board.CalculateReward();
        AddReward(reward);
        Debug.Log("Reward: "+reward);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Mathf.RoundToInt(Input.GetAxis("Horizontal")); // -1: Left, 0: No movement, 1: Right
        discreteActions[1] = Mathf.RoundToInt(Input.GetAxis("Vertical"));   // 0: No rotation, 1: Clockwise, -1: Counterclockwise
        continuousActions[0] = Input.GetKey(KeyCode.Space) ? 1.0f : 0.0f; // 1: Drop, 0: No drop
    }
}
