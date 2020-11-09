using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BladesOfBellevue
{
    public class TeleporterNodeHelper : MonoBehaviour
    {
        public DistrictType district;

        public Node myNode;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;

            Gizmos.DrawWireCube(gameObject.transform.position, gameObject.transform.lossyScale);
        }
    }
}

