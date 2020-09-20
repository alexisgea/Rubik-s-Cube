using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// IDEAS - TODOS
//

// add a leaderboard and timers
// add gold star for current record and grey start for past record

// add key shortcut for solving the cube (need back, rings and cube flip)
// add personalisable keys (and settings for sounds, speed and such)
// add double layer turns (lower cases)

// add better lookaround -> improve vertical still

// When moving, push the cam/cube slightly on the side to give some impact
// Add a satisfying win sound with a nice SOLVED + time + leaderboard drop and camshake
// add some more smoothing and variance to the rolls to feel more natural
// make a nicer cube
// add a nice background -> sky box, maybe a table?
// add some background music  + sound ambiance?

// add ring highlights on button hover
// add mouse movements with ring highlights

// add controller support with vibration


// NOTE
// get shuffle as string and process it


// LEADERBOARD CONCEPT
// to start a ranked solve
// you notify the server you want to start
// the server sends you a shuffle and your id and keeps track of the id + time + shuffle
// On reception, you will be given 15 seconds before the sovle starts
// the game will lock controls during the 15 seconds
// then you can start the solve and the timer will start automatically
// on solve it will send to the server your id, your moves and your time
// the server will check that your moves do result in a solve
// if you time is close to the server time, it will accept your time (so it's nicer for you)
// maybe the server will check the ping response time at each exchange and use that as a variable to check the validity of your time
// it will aslo do a sum of the moves with the turning speed of the game as the bottom line for the tsolve time


public enum Face {Up, Front, Right, Back, Left, Down, Equator, Middle, Standing, CubeY, CubeX, CubeZ}

public struct FaceMove {
    public Face Face;
    public bool Prime;
    public bool Double;
    public float Remaining;
    public bool Hidden;
    public bool Shuffle;
    public bool Log;

    public override string ToString() {
        string notation = "";

        switch (this.Face)
        {
            case Face.Up:
                notation = "U"; break;

            case Face.Down:
                notation = "D"; break;

            case Face.Right:
                notation = "R"; break;

            case Face.Left:
                notation = "L"; break;

            case Face.Front:
                notation = "F"; break;

            case Face.Back:
                notation = "B"; break;

            case Face.Equator:
                notation = "E"; break;

            case Face.Middle:
                notation = "M"; break;

            case Face.Standing:
                notation = "S"; break;

            case Face.CubeY:
                notation = "Y"; break;

            case Face.CubeX:
                notation = "X"; break;

            case Face.CubeZ:
                notation = "Z"; break;
        }

        if(this.Double) notation += "2";
        else if(this.Prime) notation += "'";

        return notation;
    }
}

public class RubiksCube : MonoBehaviour
{
    public int[] VirtualCube {get{return _virtualCube;}}

    [SerializeField] AudioClip[] _turnSounds;
    [SerializeField] AudioClip[] _flipSounds;
    private AudioSource _audioSource;



    private FaceMove _currentMove;
    public FaceMove[] PlannedMoves {get { return _plannedMoves.ToArray(); }}
    private List<FaceMove> _plannedMoves = new List<FaceMove>();
    public FaceMove[] PreviousMoves {get { return _previousMoves.ToArray(); }}
    private Stack<FaceMove> _previousMoves = new Stack<FaceMove>();
    public FaceMove[] ShuffleMoves {get { return _shuffleMoves.ToArray(); }}
    private Stack<FaceMove> _shuffleMoves = new Stack<FaceMove>();

    private float _rotationThreashold = 0.1f;
    private float _rotationLerpOffset = 10f;
    private float _rotationSpeed = 10f;


    // phyical cube plan
    //
    // -- -- 00 01 02
    // -- 03 04 05
    // 06 07 08
    // -- -- 09 10 11
    // -- 12 13 14
    // 15 16 17
    // -- -- 18 19 20
    // -- 21 22 23
    // 24 25 26

    /// <summary>
    /// The physical cube is the collection of all small-cubes, 27 of them.
    /// </summary>
    private SmallCube[] _physicalCube = new SmallCube[27];
    private int[] _phUpFaceInd = new int[] {0, 1, 2, 3, 4, 5, 6, 7, 8};
    private int[] _phFrontFaceInd = new int[] {6, 7, 8, 15, 16, 17, 24, 25, 26};
    private int[] _phRightFaceInd = new int[] {8, 5, 2, 17, 14, 11, 26, 23, 20};
    private int[] _phBackFaceInd = new int[] {2, 1, 0, 11, 10, 9, 20, 19, 18};
    private int[] _phLeftFaceInd = new int[] {0, 3, 6, 9, 12, 15, 18, 21, 24};
    private int[] _phDownFaceInd = new int[] {24, 25, 26, 21, 22, 23, 18, 19, 20};

    private int[] _phEquatorLayerInd = new int[] {11, 10, 9, 14, 13, 12, 17, 16, 15};
    private int[] _phMiddleLayerInd = new int[] {1, 4, 7, 10, 13, 16, 19, 22, 25};
    private int[] _phStandingLayerInd = new int[] {3, 4, 5, 12, 13, 14, 21, 22, 23};
    private int[] _phAllIndexes = new int[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13,
        14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26};

    // virtual cube plan
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

    /// <summary>
    /// The virtual cube is the collection of each small-cube's faces separately, 54 of them.
    /// Each face contains an int corresponding to it's color id (0 - 5)
    /// </summary>
    private int[] _virtualCube;
    private int[] _vrRingUpInd = new int[12] {9, 10, 11, 18, 19, 20, 27, 28, 29, 36, 37, 38};
    private int[] _vrRingFrontInd = new int[12] {45, 46, 47, 24, 21, 18, 8, 7, 6, 38, 41, 44};
    private int[] _vrRingRightInd = new int[12] {47, 50, 53, 33, 30, 27, 2, 5, 8, 11, 14, 17};
    private int[] _vrRingBackInd = new int[12] {53, 52, 51, 42, 39, 36, 0, 1, 2, 20, 23, 26};
    private int[] _vrRingLeftInd = new int[12] {51, 48, 45, 15, 12, 9, 6, 3, 0, 29, 32, 35};
    private int[] _vrRingDownInd = new int[12] {35, 34, 33, 26, 25, 24, 17, 16, 15, 44, 43, 42};

    private int[] _vrRingEquatorInd = new int[12] {41, 40, 39, 32, 31, 30, 23, 22, 21, 14, 13, 12};
    private int[] _vrRingMiddleInd = new int[12] {16, 13, 10, 7, 4, 1, 28, 31, 34, 52, 49, 46};
    private int[] _vrRingParallelInd = new int[12] {48, 49, 50, 25, 22, 19, 5, 4, 3, 37, 40, 43};


    public void RotateFace(Face face, bool prime, bool alreadyDouble = false, bool shuffle = false, bool hidden = false, bool log = true) {

        var newMove = new FaceMove(){Face = face, Prime = prime, Remaining = alreadyDouble ? 180f : 90f,
                Double = alreadyDouble, Shuffle = shuffle, Hidden = hidden, Log = log};
        
        if(GeneralSettings.NoDoubleMove) {
            _plannedMoves.Add(newMove);
            return;
        }

        // Doubling a planned move
        if(!alreadyDouble && _plannedMoves.Count > 0 && !_plannedMoves[_plannedMoves.Count -1].Double &&
        _plannedMoves[_plannedMoves.Count -1].Face == face && _plannedMoves[_plannedMoves.Count -1].Prime == prime) {

            var move = _plannedMoves[_plannedMoves.Count -1];
            move.Remaining += 90f;
            move.Double = true;
            _plannedMoves[_plannedMoves.Count -1] = move;

        }
        // doubling the ongoing move
        else if(!alreadyDouble && _plannedMoves.Count == 0 && _currentMove.Remaining > 0 && !_currentMove.Double &&
        _currentMove.Face == face && _currentMove.Prime == prime){

            _currentMove.Remaining += 90f;
            _currentMove.Double = true;

            // same move but on a currently happening move, so we need to process it once more
            ProcessQuarterMove(_currentMove);
            if(_currentMove.Shuffle && _currentMove.Log){
                var tempMove = _shuffleMoves.Pop();
                tempMove.Double = true;
                tempMove.Remaining = 180f;
                _shuffleMoves.Push(tempMove);
            }
            else if(_currentMove.Log){
                var tempMove = _previousMoves.Pop();
                tempMove.Double = true;
                tempMove.Remaining = 180f;
                _previousMoves.Push(tempMove);
            }

        }
        // normal move
        else {
            _plannedMoves.Add(newMove);
        }
    }

    public void ReverseAll() {
        if(IsSolved()) {
            _plannedMoves.Clear();
            _previousMoves.Clear();
            _shuffleMoves.Clear();
        }
        else {
            ReverseManualMoves();
            ReverseShuffleMoves();
        }
    }

    public void ReverseToShuffle() {
        ReverseManualMoves();
    }

    public void ReverseManualMoves() {
        _plannedMoves.Clear();

        var moves = _previousMoves;

        foreach (var move in moves)
        {
            RotateFace(move.Face, !move.Prime, alreadyDouble:move.Double, log:false);
        }

        _previousMoves.Clear();
    }


    public void ReverseLast() {
        _plannedMoves.Clear();
        if(_previousMoves.Count > 0) {
            var moveToReverse = _previousMoves.Pop();
            RotateFace(moveToReverse.Face, !moveToReverse.Prime, alreadyDouble:moveToReverse.Double, log:false);
        }
    }

    public bool IsSolved() {
        for(int f = 0; f < 6; f++) {
            int f_p = f * 9;
            int valCheck = _virtualCube[f_p];
            for(int i = 0; i < 9; i++) {
                if(_virtualCube[f_p + i] != valCheck) return false;
            }
        }

        return true;
    }

    public List<FaceMove> ProcessInputString(string inputs) {
        List<FaceMove> moves = new List<FaceMove>();
        var stringMoves = inputs.Split(' ');

        foreach(var sm in stringMoves) {
            var move = new FaceMove();
            move.Shuffle = true;
            move.Log = false;
            move.Hidden = false;

            bool valid = true;
            
            if(sm.Length == 0) {
                continue;
            }

            // check face
            if(sm[0] == 'R') {
                move.Face = Face.Right;
            }
            else if(sm[0] == 'L') {
                move.Face = Face.Left;
            }
            else if(sm[0] == 'U') {
                move.Face = Face.Up;
            }
            else if(sm[0] == 'D') {
                move.Face = Face.Down;
            }
            else if(sm[0] == 'B') {
                move.Face = Face.Back;
            }
            else if(sm[0] == 'F') {
                move.Face = Face.Front;
            }
            else if(sm[0] == 'E') {
                move.Face = Face.Equator;
            }
            else if(sm[0] == 'S') {
                move.Face = Face.Standing;
            }
            else if(sm[0] == 'M') {
                move.Face = Face.Middle;
            }
            else if(sm[0] == 'X') {
                move.Face = Face.CubeX;
            }
            else if(sm[0] == 'Y') {
                move.Face = Face.CubeY;
            }
            else if(sm[0] == 'Z') {
                move.Face = Face.CubeZ;
            }
            else {
                valid = false;
            }

            // check prime
            if(sm.Contains("'")) {
                move.Prime = true;
            }

            // check double
            if(sm.Contains("2")) {
                move.Double = true;
                move.Remaining = 180f;
            }
            else {
                move.Remaining = 90f;
            }

            if(valid) {
                moves.Add(move);
            }
            else{
                Debug.LogError("Cannot identify move: " + sm);
            }
        }
        return moves;
    }


    private void Start() {
        _audioSource = GetComponent<AudioSource>();

        // initialise cube
        _virtualCube = new int[54];
        for(int i = 0, c = 0; i < 6; i ++) {
            for(int j = 0; j < 9 && c < 54; j++, c++) {
                _virtualCube[c] = i;
            }
        }

        _physicalCube = GetComponentsInChildren<SmallCube>();
    }

    private void Update() {

        if(_currentMove.Remaining > 0) {

            if((!_currentMove.Double && _currentMove.Remaining == 90f )|| (_currentMove.Double && _currentMove.Remaining == 180f)) NewMoveStarted(_currentMove);

            var cubeIndexes = GetPhysicalFaceCubeIndexes(_currentMove.Face);
            var rotationAxis = GetRotationAxis(_currentMove);
            var worldAxis = transform.TransformDirection(rotationAxis);

            var rotation = Mathf.Lerp(0, _currentMove.Remaining + _rotationLerpOffset, _rotationSpeed * Time.deltaTime);

            if(rotation < _rotationThreashold || rotation > _currentMove.Remaining) {
                rotation = _currentMove.Remaining;
            }

            _currentMove.Remaining -= rotation;

            foreach (var i in cubeIndexes)
            {
                var localAxis = _physicalCube[i].transform.InverseTransformDirection(worldAxis);
                _physicalCube[i].transform.Rotate(localAxis, rotation);
            }
        }

        if(_plannedMoves.Count > 0 && _currentMove.Remaining == 0) {
            _currentMove = _plannedMoves[0];
            _plannedMoves.RemoveAt(0);
        }
    }

    private void ReverseShuffleMoves() {
        var moves = _shuffleMoves;

        foreach (var move in moves)
        {
            RotateFace(move.Face, !move.Prime, alreadyDouble:move.Double, log:false);
        }

        _shuffleMoves.Clear();
    }


    private void NewMoveStarted(FaceMove move) {
        if(move.Face == Face.CubeY || move.Face == Face.CubeX) {
            _audioSource.PlayOneShot(_flipSounds[Random.Range(0, _flipSounds.Length)]);
        }
        else {
            _audioSource.PlayOneShot(_turnSounds[Random.Range(0, _turnSounds.Length)]);
        }

        // Perhaps this should be moved to the end or middle of the physical move?t
        ProcessQuarterMove(move);
        if(move.Double) ProcessQuarterMove(move);
        
        if(move.Shuffle && move.Log) _shuffleMoves.Push(move);
        else if(move.Log) _previousMoves.Push(move);
    }

    private void ProcessQuarterMove(FaceMove move) {
        if(move.Face == Face.Equator || move.Face == Face.Middle || move.Face == Face.Standing) {
            // ring rotation
            PhysicalFaceRotation(move.Face, move.Prime);
            VirtualRingRotation(GetVituralRingIndexes(move.Face), move.Prime);
        }
        else if(move.Face == Face.CubeY) {
            // horizontal cube rotation
            PhysicalFaceRotation(Face.Up, move.Prime);
            PhysicalFaceRotation(Face.Equator, !move.Prime);
            PhysicalFaceRotation(Face.Down, !move.Prime);

            VirtualFaceRotation(Face.Up, move.Prime);
            VirtualRingRotation(GetVituralRingIndexes(Face.Equator), !move.Prime);
            VirtualFaceRotation(Face.Down, !move.Prime);
        }
        else if(move.Face == Face.CubeX) {
            // vertical cube rotation
            PhysicalFaceRotation(Face.Right, move.Prime);
            PhysicalFaceRotation(Face.Middle, !move.Prime);
            PhysicalFaceRotation(Face.Left, !move.Prime);

            VirtualFaceRotation(Face.Right, move.Prime);
            VirtualRingRotation(GetVituralRingIndexes(Face.Middle), !move.Prime);
            VirtualFaceRotation(Face.Left, !move.Prime);
        }
        else if(move.Face == Face.CubeZ) {
            // vertical cube rotation
            PhysicalFaceRotation(Face.Front, move.Prime);
            PhysicalFaceRotation(Face.Standing, move.Prime);
            PhysicalFaceRotation(Face.Back, !move.Prime);

            VirtualFaceRotation(Face.Front, move.Prime);
            VirtualRingRotation(GetVituralRingIndexes(Face.Standing), move.Prime);
            VirtualFaceRotation(Face.Back, !move.Prime);
        }
        else {
            // standard rotation
            PhysicalFaceRotation(move.Face, move.Prime);
            VirtualFaceRotation(move.Face, move.Prime);
        }
    }

    private Vector3 GetRotationAxis(FaceMove rotation) {

        switch (rotation.Face)
        {
            case Face.Up:
            case Face.CubeY:
                return rotation.Prime ? Vector3.down : Vector3.up;

            case Face.Down:
            case Face.Equator:
                return rotation.Prime ? Vector3.up : Vector3.down;

            case Face.Right:
            case Face.CubeX:
                return rotation.Prime ? Vector3.left : Vector3.right;

            case Face.Left:
            case Face.Middle:
                return rotation.Prime ? Vector3.right : Vector3.left;

            case Face.Front:
            case Face.Standing:
            case Face.CubeZ:
                return rotation.Prime ? Vector3.forward : Vector3.back;

            case Face.Back:
                return rotation.Prime ? Vector3.back : Vector3.forward;

            default:
                return Vector3.zero;
        }
    }

    private int[] GetPhysicalFaceCubeIndexes(Face face) {
        switch (face)
        {
            case Face.Up: return _phUpFaceInd;
            case Face.Down: return _phDownFaceInd;
            case Face.Right: return _phRightFaceInd;
            case Face.Left: return _phLeftFaceInd;
            case Face.Front: return _phFrontFaceInd;
            case Face.Back: return _phBackFaceInd;

            case Face.Middle: return _phMiddleLayerInd;
            case Face.Equator: return _phEquatorLayerInd;
            case Face.Standing: return _phStandingLayerInd;

            default: return _phAllIndexes;
        }
    }


    private void PhysicalFaceRotation(Face face, bool prime) {
        SmallCube[] tempFaceCubes = new SmallCube[9];
        var faceCubeIndexes = GetPhysicalFaceCubeIndexes(face);

        for(int i = 0; i < 9; i++) {
            tempFaceCubes[i] = _physicalCube[faceCubeIndexes[i]];
        }

        _physicalCube[faceCubeIndexes[0]] = prime ? tempFaceCubes[2] : tempFaceCubes[6];
        _physicalCube[faceCubeIndexes[1]] = prime ? tempFaceCubes[5] : tempFaceCubes[3];

        _physicalCube[faceCubeIndexes[2]] = prime ? tempFaceCubes[8] : tempFaceCubes[0];
        _physicalCube[faceCubeIndexes[5]] = prime ? tempFaceCubes[7] : tempFaceCubes[1];

        _physicalCube[faceCubeIndexes[8]] = prime ? tempFaceCubes[6] : tempFaceCubes[2];
        _physicalCube[faceCubeIndexes[7]] = prime ? tempFaceCubes[3] : tempFaceCubes[5];

        _physicalCube[faceCubeIndexes[6]] = prime ? tempFaceCubes[0] : tempFaceCubes[8];
        _physicalCube[faceCubeIndexes[3]] = prime ? tempFaceCubes[1] : tempFaceCubes[7];
    }

    private void VirtualFaceRotation(Face face, bool prime) {
        int[] tempFaceValues = new int[9]; // todo, could replace by Array.Copy or something similar
        int f_i = (int)face * 9;

        for(int i = 0; i < 9; i++) {
            tempFaceValues[i] = _virtualCube[f_i + i];
        }

        _virtualCube[f_i + 0] = prime ? tempFaceValues[2] : tempFaceValues[6];
        _virtualCube[f_i + 1] = prime ? tempFaceValues[5] : tempFaceValues[3];

        _virtualCube[f_i + 2] = prime ? tempFaceValues[8] : tempFaceValues[0];
        _virtualCube[f_i + 5] = prime ? tempFaceValues[7] : tempFaceValues[1];

        _virtualCube[f_i + 8] = prime ? tempFaceValues[6] : tempFaceValues[2];
        _virtualCube[f_i + 7] = prime ? tempFaceValues[3] : tempFaceValues[5];

        _virtualCube[f_i + 6] = prime ? tempFaceValues[0] : tempFaceValues[8];
        _virtualCube[f_i + 3] = prime ? tempFaceValues[1] : tempFaceValues[7];

        VirtualRingRotation(GetVituralRingIndexes(face), prime);
    }

    private void VirtualRingRotation(int[] ringIndexes, bool prime) {
        // we temporarily store and already shift the values at the given indexes
        int[] shiftedValues = new int[ringIndexes.Length];
        for(int i = 0; i < ringIndexes.Length; i++) {
            int r_i = (prime ? (i - 3) : (i + 3)) % ringIndexes.Length;
            if(r_i < 0) r_i += ringIndexes.Length;
            shiftedValues[i] = _virtualCube[ringIndexes[r_i]];
        }

        // we attribute the shifted values back at the given indexes
        for(int i = 0; i < ringIndexes.Length; i++) {
            _virtualCube[ringIndexes[i]] = shiftedValues[i];
        }
    }

    private int[] GetVituralRingIndexes(Face rotatingFace) {

        switch (rotatingFace)
        {
            case Face.Up :          return _vrRingUpInd;
            case Face.Front :       return _vrRingFrontInd;
            case Face.Right :       return _vrRingRightInd;
            case Face.Back :        return _vrRingBackInd;
            case Face.Left :        return _vrRingLeftInd;
            case Face.Down :        return _vrRingDownInd;

            case Face.Equator :  return _vrRingEquatorInd;
            case Face.Middle :    return _vrRingMiddleInd;
            case Face.Standing :    return _vrRingParallelInd;

            default: return new int[12];
        }
    }
}
