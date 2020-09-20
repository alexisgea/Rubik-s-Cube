using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Text;


// Timer Flow


// more notes
// track time per solve step
// track own best time and make difference with it
// enable ability to start timer during casula stuff


public enum ControlPhase {Idle, Suffling, Prepping, Solving, Solved}
public class ControlButtons : MonoBehaviour
{
    [SerializeField] Text _moves;
    [SerializeField] Transform _cubeMapParent;
    [SerializeField] Transform _cubeParent;
    [SerializeField] Transform _cylinder;
    [SerializeField] Text _timerBox;
    [SerializeField] GameObject _solvePanel;
    [SerializeField] Transform[] _locakbleButtonGroups;

    private Vector3 _cylinderDefaultPosition = new Vector3(0, 10, 0);
    private Vector3 _cylinderHidePosition = new Vector3(0, 0.3f, 0);


    private bool _controlLocked = false;
    private bool _shuffling = false;
    private bool _solving = false;
    private bool _prepping = false;
    private bool _casual = true;

    

    private float _prepTime = 15f;
    private float _timer = 0f;


    private RubiksCube _rCube;
    private Image[] _cubeMap;

    private Quaternion _startCubeParentRotation;
    private Quaternion _startCubeRotation;

    private float _cubeRotationSpeed = 10f;
    private float _cubeRotationResetSpeed = 10f;
    private Vector3 _startMouseDragPosition;
    private float _cubeRotationSensitivity = 100f;

    private Color[] _faceColors = new Color[6] {Color.white, new Color(1, 0.5f, 0, 1), Color.green, Color.red, new Color(0, 0.5f, 1, 1), Color.yellow};
    private string _space = "  ";



    private void Start() {
        _startCubeParentRotation = _cubeParent.localRotation;
        _rCube = _cubeParent.GetComponentInChildren<RubiksCube>();
        _startCubeRotation = _rCube.transform.localRotation;

        // get cube map tiles
        _cubeMap = new Image[_rCube.VirtualCube.Length];
        for(int f = 0, i = 0; f < _cubeMapParent.childCount; f++) {
            var face = _cubeMapParent.GetChild(f);
            for(int t = 0; t < face.childCount; t++, i++) {
                _cubeMap[i] = face.GetChild(t).GetComponent<Image>();
            }
        }

        // cube map coloring
        for(int i = 0; i < _cubeMap.Length; i++) {
            _cubeMap[i].color = _faceColors[_rCube.VirtualCube[i]];
            _cubeMap[i].gameObject.GetComponentInChildren<Text>().text = "";
            // _cubeMap[i].gameObject.GetComponentInChildren<Text>().text = i.ToString();
        }
    }

    private void Update() {

        // updating move text
        UpdateMoveText();


        // cube map coloring
        for(int i = 0; i < _cubeMap.Length; i++) {
            _cubeMap[i].color = _shuffling ? Color.black : _faceColors[_rCube.VirtualCube[i]];
        }

        // cube look around
        var mousePosNormalized = new Vector3(Input.mousePosition.x / (float)Screen.width, Input.mousePosition.y / (float)Screen.height, 0);
        if(Input.GetMouseButtonDown(1)) {
            _startMouseDragPosition = mousePosNormalized;
        }
        else if(Input.GetMouseButton(1)){
            var mouseDelta = mousePosNormalized - _startMouseDragPosition;
            mouseDelta *= _cubeRotationSensitivity;
            
            // something to do with trigo, I just don't know
            var targetCubeParentRotation = Quaternion.Euler(mouseDelta.y * 55f / 45f, 0, mouseDelta.y * 35f / 45f);
            _cubeParent.localRotation = Quaternion.Lerp(_cubeParent.localRotation, targetCubeParentRotation, _cubeRotationSpeed * Time.deltaTime);

            var targetCubeRotation = Quaternion.Euler(0, -mouseDelta.x * 4f, 0);
            _rCube.transform.localRotation = Quaternion.Lerp(_rCube.transform.localRotation, targetCubeRotation, _cubeRotationSpeed * Time.deltaTime);
        }
        else {
            _cubeParent.localRotation = Quaternion.Lerp(_cubeParent.localRotation, _startCubeParentRotation, _cubeRotationResetSpeed * Time.deltaTime);
            _rCube.transform.localRotation = Quaternion.Lerp(_rCube.transform.localRotation, _startCubeRotation, _cubeRotationResetSpeed * Time.deltaTime);
        }


        if(_shuffling && _rCube.PlannedMoves.Length == 0) {
            _shuffling = false;
            _cylinder.gameObject.SetActive(false);
        }
        
        // update solving state and control lock
        if(_controlLocked && _rCube.PlannedMoves.Length == 0 && !_shuffling && !_prepping) {
            _controlLocked = false;
            ToggleButtonLock(_controlLocked);
        }

        //update timer and text
        if(_prepping && !_shuffling) {
            _timer -= Time.deltaTime;

            if(_timer <= 0) {
                _timer = 0;
                _prepping = false;
                _solving = true;
            }
        }

        if(_solving && !_rCube.IsSolved()){
            _timer += Time.deltaTime;
        }
        _timerBox.text = System.TimeSpan.FromSeconds(_timer).ToString(@"mm\:ss\.fff");

        // time-out if it's taking too long
        if(_timer > 3600) {
            Reset();
        }
        
        // check solve status
        bool solved = _solving && _rCube.IsSolved();
        _solvePanel.SetActive(solved);

        if(solved && !_controlLocked) {
            _controlLocked = solved;
            ToggleButtonLock(_controlLocked);
        }

        if(solved && Input.GetMouseButtonDown(0)) {
            Reset();
        }


        // Key controls
        if(!_controlLocked) {
            // keyboard controls
            if(Input.GetKeyDown(KeyCode.U)) {
                RotateUp(prime:false);
            }
            if(Input.GetKeyDown(KeyCode.R)) {
                RotateUp(prime:true);
            }

            if(Input.GetKeyDown(KeyCode.J)) {
                RotateFront(prime:false);
            }
            if(Input.GetKeyDown(KeyCode.F)) {
                RotateFront(prime:true);
            }

            if(Input.GetKeyDown(KeyCode.I)) {
                RotateRight(prime:false);
            }
            if(Input.GetKeyDown(KeyCode.K)) {
                RotateRight(prime:true);
            }

            if(Input.GetKeyDown(KeyCode.E)) {
                RotateLeft(prime:false);
            }
            if(Input.GetKeyDown(KeyCode.D)) {
                RotateLeft(prime:true);
            }

            if(Input.GetKeyDown(KeyCode.L)) {
                RotateDown(prime:false);
            }
            if(Input.GetKeyDown(KeyCode.S)) {
                RotateDown(prime:true);
            }



            if(Input.GetKeyDown(KeyCode.UpArrow)) {
                RotateCubeVertical(prime:false);
            }
            if(Input.GetKeyDown(KeyCode.DownArrow)) {
                RotateCubeVertical(prime:true);
            }

            if(Input.GetKeyDown(KeyCode.RightArrow)) {
                RotateCubeHorizontal(prime:false);
            }
            if(Input.GetKeyDown(KeyCode.LeftArrow)) {
                RotateCubeHorizontal(prime:true);
            }
        }
    }


    private void UpdateMoveText() {
        StringBuilder moves = new StringBuilder();

        if((_rCube.ShuffleMoves.Length > 0 && !_rCube.ShuffleMoves[0].Hidden || _rCube.ShuffleMoves.Length == 0)) {
            moves.AppendLine($"Shuffle: {_rCube.ShuffleMoves.Length}");
            foreach (var move in _rCube.ShuffleMoves.Reverse())
            {
                moves.Append(move.ToString());
                moves.Append(_space);
            }
        }
        else {
            moves.AppendLine("Shuffle: Hidden");
        }

        moves.AppendLine("");
        moves.AppendLine($"Solve: {_rCube.PreviousMoves.Length}");
        foreach (var move in _rCube.PreviousMoves.Reverse())
        {
            moves.Append(move.ToString());
            moves.Append(_space);
        }

        moves.AppendLine("");
        if((_rCube.PlannedMoves.Length > 0 && !_rCube.PlannedMoves[0].Hidden || _rCube.PlannedMoves.Length == 0)) {
            moves.AppendLine($"Planned: {_rCube.PlannedMoves.Length}");
            foreach (var move in _rCube.PlannedMoves)
            {
                moves.Append(move.ToString());
                moves.Append(_space);
            }
        }
        else {
            moves.AppendLine("Planned: Hidden");
        }

        _moves.text = moves.ToString();
    }

    private void ToggleButtonLock(bool locked) {
        foreach(var group in _locakbleButtonGroups) {
            foreach(var button in group.GetComponentsInChildren<Button>()) {
                button.interactable = !locked;
            }
        }
    }


    public void CasualShuffle() {
        // _shuffling = true;
        _controlLocked = true;
        ToggleButtonLock(_controlLocked);

        _rCube.ReverseAll();

        var lastMove = new FaceMove();

        for(int i = 0; i < 20; i++) {
            Face face = (Face)Random.Range(0, 6); // ignoring middle rings
            bool prime = Random.value < 0.5f;

            if(i > 0 && lastMove.Face == face && lastMove.Prime != prime) {
                i -= 1;
            }
            else {
                lastMove = new FaceMove() {Face = face, Prime = prime};
                _rCube.RotateFace(face, prime, shuffle:true);
            }
        }
    }

    public void RankedShuffle() {
        _cylinder.gameObject.SetActive(true);

        _timer = _prepTime;
        _prepping = true;
        _shuffling = true;
        _controlLocked = true;
        ToggleButtonLock(_controlLocked);

        _rCube.ReverseAll();

        var lastMove = new FaceMove();

        for(int i = 0; i < 20; i++) {
            Face face = (Face)Random.Range(0, 6); // ignoring middle rings
            bool prime = Random.value < 0.5f;

            if(i > 0 && lastMove.Face == face && lastMove.Prime != prime) {
                i -= 1;
            }
            else {
                lastMove = new FaceMove() {Face = face, Prime = prime};
                _rCube.RotateFace(face, prime, shuffle:true, hidden:true);
            }
        }

    }

    public void Back() {
        _controlLocked = true;
        ToggleButtonLock(_controlLocked);
        _rCube.ReverseLast();
    }

    public void Solve() {

    }

    public void Reset() {
        _rCube.ReverseAll();
        _solving = false;
        _prepping = false;
        // _shuffling = true;
        _controlLocked = true;
        ToggleButtonLock(_controlLocked);
        _timer = 0;
    }

    public void RotateCubeVertical(bool prime) {
        _rCube.RotateFace(Face.CubeX, prime);
    }
    
    public void RotateCubeHorizontal(bool prime) {
        _rCube.RotateFace(Face.CubeY, prime);
    }


    public void RotateUp(bool prime) {
        _rCube.RotateFace(Face.Up, prime);
    }

    public void RotateDown(bool prime) {
        _rCube.RotateFace(Face.Down, prime);
    }

    public void RotateRight(bool prime) {
        _rCube.RotateFace(Face.Right, prime);
    }

    public void RotateLeft(bool prime) {
        _rCube.RotateFace(Face.Left, prime);
    }

    public void RotateFront(bool prime) {
        _rCube.RotateFace(Face.Front, prime);
    }


    public void RotateBack(bool prime) {
        _rCube.RotateFace(Face.Back, prime);
    }

    public void RotateHorizontal(bool prime) {
        _rCube.RotateFace(Face.Equator, prime);
    }

    public void RotateVertical(bool prime) {
        _rCube.RotateFace(Face.Middle, prime);
    }

    public void RotateParallel(bool prime) {
        _rCube.RotateFace(Face.Standing, prime);
    }

}
