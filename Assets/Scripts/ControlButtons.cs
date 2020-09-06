using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


public class ControlButtons : MonoBehaviour
{
    [SerializeField] Text _moves;
    [SerializeField] Transform _cubeMapParent;

    private RubiksCube _rCube;
    private Image[] _cubeMap;

    private Quaternion _startRCubeRotation;
    private float _cubeRotationSpeed = 10f;
    private float _cubeRotationResetSpeed = 10f;
    private Vector3 _startMouseDragPosition;
    private float _cubeRotationSensitivity = 100f;

    private Color[] _faceColors = new Color[6] {Color.white, new Color(1, 0.5f, 0, 1), Color.green, Color.red, new Color(0, 0.5f, 1, 1), Color.yellow};


    private void Start() {
        _rCube = FindObjectOfType<RubiksCube>();
        _startRCubeRotation = _rCube.transform.rotation;

        // get cube map tiles
        _cubeMap = new Image[_rCube.VirtualCube.Length];
        for(int f = 0, i = 0; f < _cubeMapParent.childCount; f++) {
            var face = _cubeMapParent.GetChild(f);
            for(int t = 0; t < face.childCount; t++, i++) {
                _cubeMap[i] = face.GetChild(t).GetComponent<Image>();
            }
        }
    }

    private void Update() {

        // updating move text
        string moves = "";
        foreach (var move in _rCube.PreviousMoves.Reverse())
        {
            moves += move.ToString() + " ";
        }
        _moves.text = moves;

        // cube look around
        var mousePosNormalized = new Vector3(Input.mousePosition.x / (float)Screen.width, Input.mousePosition.y / (float)Screen.height, 0);
        if(Input.GetMouseButtonDown(1)) {
            _startMouseDragPosition = mousePosNormalized;
        }
        else if(Input.GetMouseButton(1)){
            var mouseDelta = mousePosNormalized - _startMouseDragPosition;
            mouseDelta *= _cubeRotationSensitivity;

            var targetCubeRotation = Quaternion.Euler(mouseDelta.y, -mouseDelta.x * 4f, mouseDelta.y);
            _rCube.transform.rotation = Quaternion.Lerp(_rCube.transform.rotation, targetCubeRotation, _cubeRotationSpeed * Time.deltaTime);
        }
        else {
            _rCube.transform.rotation = Quaternion.Lerp(_rCube.transform.rotation, _startRCubeRotation, _cubeRotationResetSpeed * Time.deltaTime);
        }

        // cube map coloring
        for(int i = 0; i < _cubeMap.Length; i++) {
            _cubeMap[i].color = _faceColors[_rCube.VirtualCube[i]];

            _cubeMap[i].gameObject.GetComponentInChildren<Text>().text = i.ToString();
        }

        // if(Input.Key)
    }


    public void Shuffle() {

        var lastMove = new FaceMove();

        for(int i = 0; i < 20; i++) {
            Face face = (Face)Random.Range(0, 6); // ignoring middle rings
            bool prime = Random.value < 0.5f;

            if(i > 0 && lastMove.Face == face && lastMove.Prime != prime) {
                i -= 1;
            }
            else {
                lastMove = new FaceMove() {Face = face, Prime = prime};
                _rCube.RotateFace(face, prime, hidden:true);
            }
        }
    }

    public void Back() {
        _rCube.ReverseLast();
    }

    public void Solve() {

    }

    public void Reset() {
        _rCube.ReverseAll();
    }

    public void RotateCubeVertical(bool prime) {
        _rCube.RotateFace(Face.CubeVertical, prime);
    }
    
    public void RotateCubeHorizontal(bool prime) {
        _rCube.RotateFace(Face.CubeHorizontal, prime);
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
        _rCube.RotateFace(Face.Horizontal, prime);
    }

    public void RotateVertical(bool prime) {
        _rCube.RotateFace(Face.Vertical, prime);
    }

    public void RotateParallel(bool prime) {
        _rCube.RotateFace(Face.Parallel, prime);
    }

}
