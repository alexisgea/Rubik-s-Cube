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
    private SmallCube[] _allCubes = new SmallCube[27];

    private Queue<FaceMove> _plannedMoves = new Queue<FaceMove>();
    public FaceMove[] PreviousMoves {get { return _previousMoves.ToArray(); }}
    private Stack<FaceMove> _previousMoves = new Stack<FaceMove>();
    private Stack<FaceMove> _shuffleMoves = new Stack<FaceMove>();

    private FaceMove _currentMove;

    // private bool _rotating = false;
    // private Quaternion _targetFaceRotation;
    private float _rotationThreashold = 0.1f;
    private float _rotationLerpOffset = 10f;

    private float _rotationSpeed = 10f; // 250f
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
        _allCubes = GetComponentsInChildren<SmallCube>();
    }

    private void Update() {

        // if(!_rotating && _plannedMoves.Count > 0) {
        //     var newMove = _plannedMoves.Dequeue();

        //     var rotationAxis = GetRotationAxis(_currentMove);
        //     var worldAxis = transform.TransformDirection(rotationAxis);

        //     _targetFaceRotation = 
        //     // would possibly require to reparent child to the center cube, and rotate the center cube and then reset the center cube after deparenting...

        //     _rotating = true;

        //     if(newMove.Hidden && newMove.Log) _shuffleMoves.Push(newMove);
        //     else if(newMove.Log) _previousMoves.Push(newMove);
        // }

        // if(_rotating) {

        //     var faceCubes = GetFaceCubes(_currentMove.Face);
        // }


        if(_currentMove.Remaining > 0) {
            var faceCubes = GetFaceCubes(_currentMove.Face);
            var rotationAxis = GetRotationAxis(_currentMove);
            var worldAxis = transform.TransformDirection(rotationAxis);

            var rotation = Mathf.Lerp(0, _currentMove.Remaining + _rotationLerpOffset, _rotationSpeed * Time.deltaTime);

            // var rotation = _rotationSpeed * Time.deltaTime;
            // if(rotation > _currentMove.Remaining) {
            //     rotation = _currentMove.Remaining;
            // }
            // _currentMove.Remaining -= rotation;
            
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
            // if(_currentMove.Remaining < _rotationThreashold && IsSolved()) {
            //     Debug.LogWarning("Solved!");
            //     _plannedMoves.Clear();
            //     _previousMoves.Clear();
            //     _shuffleMoves.Clear();
            // }
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
                    if(cube.Position.z <= (1 - _errorMargin)) faceCubes.Add(cube);
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

            // if(faceCubes.Count == 9) break;
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
        foreach (var cube in _allCubes)
        {
            if(cube.Id != cube.Position) return false;
        }

        return true;
    }

    
}
