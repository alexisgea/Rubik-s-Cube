using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


public class ControlButtons : MonoBehaviour
{
    [SerializeField] Text _moves;
    [SerializeField] Transform _cubeMapParent;
    private Image[] _cubeMap;
    private RubiksCube _rCube;


    private Quaternion _startRCubeRotation;
    private Quaternion _targetCubeRotation;
    private float _cubeRotationSpeed = 10f;
    private float _cubeRotationResetSpeed = 10f;
    private Vector3 _startMouseDragPosition;
    private float _cubeRotationSensitivity = 100f;

    private Color[] _faceColors = new Color[6] {Color.white, new Color(1, 0.5f, 0, 1), Color.green, Color.red, Color.blue, Color.yellow};


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
            // _cubeMap[i].gameObject.GetComponentInChildren<Text>().text = _rCube.VirtualCube[i].ToString();
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

    public void TurnUp() {
        _rCube.RotateFace(Face.CubeVertical, prime:true);
    }

    public void TurnDown() {
        _rCube.RotateFace(Face.CubeVertical, prime:false);
    }

    public void TurnRight() {
        _rCube.RotateFace(Face.CubeHorizontal, prime:false);
    }

    public void TurnLeft() {
        _rCube.RotateFace(Face.CubeHorizontal, prime:true);
    }


    public void RotateUp() {
        _rCube.RotateFace(Face.Up, prime:false);
    }
    public void RotateUpPrime() {
        _rCube.RotateFace(Face.Up, prime:true);
    }

    public void RotateDown() {
        _rCube.RotateFace(Face.Down, prime:false);
    }
    public void RotateDownPrime() {
        _rCube.RotateFace(Face.Down, prime:true);
    }

    public void RotateRight() {
        _rCube.RotateFace(Face.Right, prime:false);
    }
    public void RotateRightPrime() {
        _rCube.RotateFace(Face.Right, prime:true);
    }

    public void RotateLeft() {
        _rCube.RotateFace(Face.Left, prime:false);
    }
    public void RotateLeftPrime() {
        _rCube.RotateFace(Face.Left, prime:true);
    }

    public void RotateFront() {
        _rCube.RotateFace(Face.Front, prime:false);
    }
    public void RotateFrontPrime() {
        _rCube.RotateFace(Face.Front, prime:true);
    }
}
