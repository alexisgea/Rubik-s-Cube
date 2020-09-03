using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// IDEAS
// When moving, push the cam/cube slightly on the side to give some impact
// add some more smoothing and variance to the rols
// add key shortcut for solving the cube
// add a leaderboard and timers
// add sounds
// make a nicer cube
// add a nice background
// add ring highlights on button
// add mouse movements with ring highlights
// add controller support with vibration
// add press space to start timer
// add gold star for current record and grey start for past record


public enum Face {Up, Down, Right, Left, Front, Back, Horizontal, Vertical, Parallel, CubeHorizontal, CubeVertical}
public struct FaceMove {
    public Face Face;
    public bool Prime;
    public float Remaining;
    public bool Hidden;
    public bool Log;

    public override string ToString() {
        switch (this.Face)
        {
            case Face.Up:
                return this.Prime ? "U'" : "U";
            
            case Face.Down:
                return this.Prime ? "D'" : "D";
            
            case Face.Right:
                return this.Prime ? "R'" : "R";
            
            case Face.Left:
                return this.Prime ? "L'" : "L";

            case Face.Front:
                return this.Prime ? "F'" : "F";
            
            case Face.Back:
                return this.Prime ? "B'" : "B";
            
            case Face.Horizontal:
                return this.Prime ? "H'" : "H";

            case Face.Vertical:
                return this.Prime ? "V'" : "V";

            case Face.Parallel:
                return this.Prime ? "P'" : "P";

            case Face.CubeHorizontal:
                return this.Prime ? "CL" : "CR";
            
            case Face.CubeVertical:
                return this.Prime ? "CD" : "CU";
            
            default:
                return "";
        }
    }
}

public class RubiksCube : MonoBehaviour
{
    //cube face ids

    // 0-0 0-1 0-2
    // 0-3 0-4 0-5
    // 0-6 0-7 0-8
    // 
    // 1-0 1-1 1-2    2-0 2-1 2-2    3-0 3-1 3-2    4-0 4-1 4-2
    // 1-3 1-4 1-5    2-3 2-4 2-5    3-3 3-4 3-5    4-3 4-4 4-5
    // 1-6 1-7 1-8    2-6 2-7 2-8    3-6 3-7 3-8    4-6 4-7 4-8
    // 
    // 5-0 5-1 5-2
    // 5-3 5-4 5-5
    // 5-6 5-7 5-8

    private enum FaceId {white = 0, green = 1, red = 2, blue = 3, orange = 4, yellow = 5}
    private int[][] _virtualCube;

    private SmallCube[] _allCubes = new SmallCube[27];

    private Queue<FaceMove> _plannedMoves = new Queue<FaceMove>();
    public FaceMove[] PreviousMoves {get { return _previousMoves.ToArray(); }}
    private Stack<FaceMove> _previousMoves = new Stack<FaceMove>();
    private Stack<FaceMove> _shuffleMoves = new Stack<FaceMove>();

    private FaceMove _currentMove;

    private float _rotationThreashold = 0.1f;
    private float _rotationLerpOffset = 10f;

    private float _rotationSpeed = 10f;
    private float _errorMargin = 0.0001f;



    public void RotateFace(Face face, bool prime, bool hidden = false, bool log = true) {
        _plannedMoves.Enqueue(new FaceMove(){Face = face, Prime = prime, Remaining = 90f, Hidden = hidden, Log = log});
    }

    public void ReverseAll() {
        _plannedMoves.Clear();

        var moves = _previousMoves;

        if(_previousMoves.Count == 0) {
            moves = _shuffleMoves;
        }

        foreach (var move in moves)
        {
            RotateFace(move.Face, !move.Prime, log:false);
        }

        if(_previousMoves.Count == 0) _shuffleMoves.Clear();
        _previousMoves.Clear();
    }

    public void ReverseLast() {
        _plannedMoves.Clear();
        var moveToReverse = _previousMoves.Pop();
        RotateFace(moveToReverse.Face, !moveToReverse.Prime, log:false);
    }


    private void Start() {
        // initialise cube
        _virtualCube = new int[6][];
        for(int i = 0; i < 6; i ++) {
            _virtualCube[i] = new int[9];
            for(int j = 0; j < 9; j++) {
                _virtualCube[i][j] = i;
            }
        }

        _allCubes = GetComponentsInChildren<SmallCube>();
    }

    private void Update() {

        if(_currentMove.Remaining > 0) {
            var faceCubes = GetFaceCubes(_currentMove.Face);
            var rotationAxis = GetRotationAxis(_currentMove);
            var worldAxis = transform.TransformDirection(rotationAxis);

            var rotation = Mathf.Lerp(0, _currentMove.Remaining + _rotationLerpOffset, _rotationSpeed * Time.deltaTime);

            if(rotation < _rotationThreashold || rotation > _currentMove.Remaining) {
                rotation = _currentMove.Remaining;
            }
            _currentMove.Remaining -= rotation;


            foreach (var cube in faceCubes)
            {
                var localAxis = cube.transform.InverseTransformDirection(worldAxis);
                cube.transform.Rotate(localAxis, rotation);
            }
            
            if(rotation < _rotationThreashold && IsSolved()) {
                Debug.LogWarning("Solved!");
                _plannedMoves.Clear();
                _previousMoves.Clear();
                _shuffleMoves.Clear();
            }
        }

        if(_currentMove.Remaining <= 0 && _plannedMoves.Count > 0) {
            _currentMove = _plannedMoves.Dequeue();

            if(_currentMove.Hidden && _currentMove.Log) _shuffleMoves.Push(_currentMove);
            else if(_currentMove.Log) _previousMoves.Push(_currentMove);
        }
    }

    private List<SmallCube> GetFaceCubes(Face face) {
        List<SmallCube> faceCubes = new List<SmallCube>();

        foreach (var cube in _allCubes)
        {
            switch (face)
            {
                case Face.Up:
                    if(cube.Position.y >= (1 - _errorMargin)) faceCubes.Add(cube);
                    break;
                
                case Face.Down:
                    if(cube.Position.y <= -(1 - _errorMargin)) faceCubes.Add(cube);
                    break;
                
                case Face.Right:
                    if(cube.Position.x >= (1 - _errorMargin)) faceCubes.Add(cube);
                    break;
                
                case Face.Left:
                    if(cube.Position.x <= -(1 - _errorMargin)) faceCubes.Add(cube);
                    break;

                case Face.Front:
                    if(cube.Position.z <= -(1 - _errorMargin)) faceCubes.Add(cube);
                    break;
                
                case Face.Back:
                    if(cube.Position.z >= (1 - _errorMargin)) faceCubes.Add(cube);
                    break;

                case Face.Vertical:
                    if(cube.Position.x == 0) faceCubes.Add(cube);
                    break;

                case Face.Horizontal:
                    if(cube.Position.y == 0) faceCubes.Add(cube);
                    break;

                case Face.Parallel:
                    if(cube.Position.z == 0) faceCubes.Add(cube);
                    break;

                case Face.CubeHorizontal:
                case Face.CubeVertical:
                    faceCubes.Add(cube);
                    break;
            }
        }

        return faceCubes;
    }

    private Vector3 GetRotationAxis(FaceMove rotation) {

        switch (rotation.Face)
        {
            case Face.Up:
            case Face.Horizontal:
            case Face.CubeHorizontal:
                return rotation.Prime ? Vector3.down : Vector3.up;
            
            case Face.Down:
                return rotation.Prime ? Vector3.up : Vector3.down;
            
            case Face.Right:
            case Face.Vertical:
            case Face.CubeVertical:
                return rotation.Prime ? Vector3.left : Vector3.right;
            
            case Face.Left:
                return rotation.Prime ? Vector3.right : Vector3.left;

            case Face.Front:
            case Face.Parallel:
                return rotation.Prime ? Vector3.forward : Vector3.back;
            
            case Face.Back:
                return rotation.Prime ? Vector3.back : Vector3.forward;
            
            default:
                return Vector3.zero;
        }
    }

    private bool IsSolved() {
        for(int i = 0; i < _virtualCube.Length; i++) {
            int valCheck = _virtualCube[i][5];
            for(int j = 0; j < _virtualCube[i].Length; j++) {
                if(_virtualCube[i][j] != valCheck) return false;
            }
        }

        return true;
    }
    
    // 0-0 0-1 0-2
    // 0-3 0-4 0-5
    // 0-6 0-7 0-8
    // 
    // 1-0 1-1 1-2    2-0 2-1 2-2    3-0 3-1 3-2    4-0 4-1 4-2
    // 1-3 1-4 1-5    2-3 2-4 2-5    3-3 3-4 3-5    4-3 4-4 4-5
    // 1-6 1-7 1-8    2-6 2-7 2-8    3-6 3-7 3-8    4-6 4-7 4-8
    // 
    // 5-0 5-1 5-2
    // 5-3 5-4 5-5
    // 5-6 5-7 5-8
    private void VirtualUp() {
        var tempUp = _virtualCube[0];
        _virtualCube[0][0] = tempUp[3];
        _virtualCube[0][1] = tempUp[0];
        _virtualCube[0][2] = tempUp[1];
        _virtualCube[0][5] = tempUp[2];
        _virtualCube[0][8] = tempUp[5];
        _virtualCube[0][7] = tempUp[8];
        _virtualCube[0][6] = tempUp[7];
        _virtualCube[0][3] = tempUp[6];

        var tempFront = _virtualCube[1];
        var tempRight = _virtualCube[2];
        var tempBack = _virtualCube[3];
        var tempLeft = _virtualCube[4];
        _virtualCube[1][0] = tempFront[1];
        _virtualCube[1][1] = tempFront[2];
        _virtualCube[1][2] = tempRight[0];
        _virtualCube[2][0] = tempRight[1];
        _virtualCube[2][1] = tempRight[2];
        _virtualCube[2][2] = tempBack[0];
        _virtualCube[2][0] = tempBack[1];
        _virtualCube[2][1] = tempBack[2];
        _virtualCube[2][2] = tempLeft[0];
        _virtualCube[3][0] = tempLeft[1];
        _virtualCube[3][1] = tempLeft[2];
        _virtualCube[3][2] = tempFront[0];
    }

}
