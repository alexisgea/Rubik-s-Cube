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

    public int[] VirtualCube {get{return _virtualCube;}}

    private SmallCube[] _allCubes = new SmallCube[27];

    private FaceMove _currentMove;
    private Queue<FaceMove> _plannedMoves = new Queue<FaceMove>();
    public FaceMove[] PreviousMoves {get { return _previousMoves.ToArray(); }}
    private Stack<FaceMove> _previousMoves = new Stack<FaceMove>();
    private Stack<FaceMove> _shuffleMoves = new Stack<FaceMove>();

    private float _rotationThreashold = 0.1f;
    private float _rotationLerpOffset = 10f;
    private float _rotationSpeed = 10f;
    private float _errorMargin = 0.0001f;

    // full cube plan
    //
    // 00 01 02
    // 03 04 05
    // 06 07 08
    //
    // 09 10 11    18 19 20    27 28 29    36 37 38
    // 12 13 14    21 22 23    30 31 32    39 40 41
    // 15 16 17    24 25 26    33 34 35    42 43 44
    //
    // 45 46 47
    // 48 49 50
    // 51 52 53
    //
    // ring indexes
    // 00 01 02    03 04 05    06 07 08    09 10 11
    private int[] _virtualCube;
    private int[] _ringUp = new int[12] {9, 10, 11, 18, 19, 20, 27, 28, 29, 36, 37, 38};
    private int[] _ringFront = new int[12] {45, 46, 47, 24, 21, 18, 8, 7, 6, 38, 41, 44};
    private int[] _ringRight = new int[12] {47, 50, 53, 33, 30, 27, 2, 5, 8, 11, 14, 17};
    private int[] _ringBack = new int[12] {53, 52, 51, 42, 39, 36, 2, 1, 0, 20, 23, 26};
    private int[] _ringLeft = new int[12] {51, 48, 45, 15, 12, 9, 6, 3, 0, 29, 32, 35};
    private int[] _ringDown = new int[12] {0, 1, 2, 26, 25, 24, 17, 16, 15, 44, 43, 42};
    private int[] _ringHorizontal = new int[12] {13, 13, 14, 21, 22, 23, 30, 31, 32, 39, 40, 41};
    private int[] _ringVertical = new int[12] {46, 49, 52, 34, 31, 28, 1, 4, 7, 10, 13, 16};
    private int[] _ringParallel = new int[12] {48, 49, 50, 25, 22, 19, 5, 4, 3, 37, 40, 43};


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
        _virtualCube = new int[54];

        for(int i = 0, c = 0; i < 6; i ++) {
            for(int j = 0; j < 9 && c < 54; j++, c++) {
                _virtualCube[c] = i;
            }
        }

        _allCubes = GetComponentsInChildren<SmallCube>();
    }

    private void SmallTest(int[] array) {
        array[0] = 10;
        array = new int[1] {50};
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

            if(_currentMove.Remaining == 0 && IsSolved()) {
                Debug.LogWarning("Solved!");
                _plannedMoves.Clear();
                _previousMoves.Clear();
                _shuffleMoves.Clear();
            }
        }

        if(_currentMove.Remaining <= 0 && _plannedMoves.Count > 0) {
            _currentMove = _plannedMoves.Dequeue();

            // TODO Special cases for ring only rotation
            // TODO special cases fur cube rotation
            // Perhaps this should be moved to the end or middle of the physical move?
            // the virtual algo is instant
            VirtualFaceRotation(_currentMove.Face, _currentMove.Prime);

            if(_currentMove.Hidden && _currentMove.Log) _shuffleMoves.Push(_currentMove);
            else if(_currentMove.Log) _previousMoves.Push(_currentMove);
        }

        // recolor test quads

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
        // Debug.LogError("checking solve");
        for(int f = 0; f < 6; f++) {
            int f_p = f * 9;
            int valCheck = _virtualCube[f_p];
            for(int i = 0; i < 9; i++) {
                // if(_virtualCube[f_p + i] != valCheck) Debug.LogError($"{f_p + i} : {_virtualCube[f_p + i]} != {f * 9} : {valCheck}");
                if(_virtualCube[f_p + i] != valCheck) return false;
            }
        }

        return true;
    }


    private void VirtualFaceRotation(Face face, bool prime) {
        int f_i = (int)face * 9;

        int[] faceValues = new int[9]; // todo, could replace by Array.Copy or something similar
        for(int i = 0; i < 9; i++) {
            faceValues[i] = _virtualCube[f_i + i];
        }

        _virtualCube[f_i + 0] = prime ? faceValues[2] : faceValues[6];
        _virtualCube[f_i + 1] = prime ? faceValues[5] : faceValues[3];

        _virtualCube[f_i + 2] = prime ? faceValues[8] : faceValues[0];
        _virtualCube[f_i + 5] = prime ? faceValues[7] : faceValues[1];

        _virtualCube[f_i + 8] = prime ? faceValues[6] : faceValues[2];
        _virtualCube[f_i + 7] = prime ? faceValues[3] : faceValues[5];

        _virtualCube[f_i + 6] = prime ? faceValues[0] : faceValues[8];
        _virtualCube[f_i + 3] = prime ? faceValues[1] : faceValues[7];

        VirtualRingRotation(GetVituralRingIndexes(face), prime);
    }

    // 00 01 02
    // 03 04 05
    // 06 07 08
    //
    // 09 10 11    18 19 20    27 28 29    36 37 38
    // 12 13 14    21 22 23    30 31 32    39 40 41
    // 15 16 17    24 25 26    33 34 35    42 43 44
    //
    // 45 46 47
    // 48 49 50
    // 51 52 53

    // {47, 50, 53, 33, 30, 27, 2, 5, 8, 11, 14, 17};
    private void VirtualRingRotation(int[] ringIndexes, bool prime) {
        // we temporarily store and already shift the values at the given indexes
        int[] shiftedValues = new int[ringIndexes.Length];
        for(int i = 0; i < ringIndexes.Length; i++) {
            int r_i = (prime ? (i - 3) : (i + 3)) % ringIndexes.Length;
            if(r_i < 0) r_i += ringIndexes.Length;

            // Debug.LogError($"{ringIndexes[i]} : {_virtualCube[ringIndexes[i]] } -> {ringIndexes[r_i]} : {_virtualCube[ringIndexes[r_i]]}");

            shiftedValues[i] = _virtualCube[ringIndexes[r_i]];
        }

        // we attribute the shifted values back at the given indexes
        for(int i = 0; i < ringIndexes.Length; i++) {
            // Debug.LogError($"{ringIndexes[i]} : {_virtualCube[ringIndexes[i]]} -> {shiftedValues[i]}");
            _virtualCube[ringIndexes[i]] = shiftedValues[i];
        }
    }

    private int[] GetVituralRingIndexes(Face rotatingFace) {

        switch (rotatingFace)
        {
            case Face.Up :          return _ringUp;
            case Face.Front :       return _ringFront;
            case Face.Right :       return _ringRight;
            case Face.Back :        return _ringBack;
            case Face.Left :        return _ringLeft;
            case Face.Down :        return _ringDown;

            case Face.Horizontal :  return _ringHorizontal;
            case Face.Vertical :    return _ringVertical;
            case Face.Parallel :    return _ringParallel;

            default: return new int[12];
        }
    }
}
