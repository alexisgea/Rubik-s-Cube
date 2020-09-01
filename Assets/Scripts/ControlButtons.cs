using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


public class ControlButtons : MonoBehaviour
{
    [SerializeField] Text _moves;
    private RubiksCube _rCube;


    private Quaternion _startRCubeRotation;
    private Quaternion _targetCubeRotation;
    private float _cubeRotationSpeed = 10f;
    private float _cubeRotationResetSpeed = 10f;
    private Vector3 _startMouseDragPosition;
    private float _cubeRotationSensitivity = 100f;


    private void Start() {
        _rCube = FindObjectOfType<RubiksCube>();
        _startRCubeRotation = _rCube.transform.rotation;
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
        
    }

    public void TurnDown() {

    }

    public void TurnRight() {
        _rCube.SmallCubeGroup.Rotate(Vector3.down, 90);
    }

    public void TurnLeft() {
        _rCube.SmallCubeGroup.Rotate(Vector3.up, 90);
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
