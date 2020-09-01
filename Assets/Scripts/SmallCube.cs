using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SmallCube : MonoBehaviour
{
   public Vector3 Id {private set; get;}
   public Vector3 Position {get{return _mainCube.InverseTransformPoint(_smallCube.position);}} 

   private Transform _smallCube;
   private Transform _mainCube;

   private void Awake() {
       _smallCube = transform.GetChild(0);
       _mainCube = transform.parent;
       Id = _smallCube.localPosition;
   }
}
