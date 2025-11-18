using UnityEngine;
using System.Collections.Generic;

namespace UXF
{
    /// <summary>
    /// Attach this component to a gameobject and assign it in the trackedObjects field in an ExperimentSession to automatically record position/rotation of the object at each frame.
    /// </summary>
    public class BallTracker : Tracker
    {
        public override string MeasurementDescriptor => "movement";

        public override IEnumerable<string> CustomHeader => new string[]
        {
            objectName + "Pos_x",
            objectName + "Pos_y",
            objectName + "Pos_z",
            objectName + "Vel_x",
            objectName + "Vel_y",
            objectName + "Vel_z",
            objectName + "Rot_x",
            objectName + "Rot_y",
            objectName + "Rot_z",
            objectName + "MeshRadius",
            objectName + "ColliderRadius"
        };

    // Remove SetupDescriptorAndHeader() method as it's no longer needed

        /// <summary>
        /// Returns current position and rotation values
        /// </summary>
        /// <returns></returns>
        protected override UXFDataRow GetCurrentValues()
        {

            // get position and rotation
            Vector3 p = gameObject.transform.position;
            Vector3 v = gameObject.GetComponent<Rigidbody>().linearVelocity;
            Vector3 r = gameObject.transform.eulerAngles;

            //transform.localScale
            float meshRadius = gameObject.transform.localScale.x;
            float colliderRadius = gameObject.GetComponent<SphereCollider>().radius;

            string format = "0.####";

            // return position, rotation (x, y, z) as an array
            var values =  new UXFDataRow()
            {
                (objectName + "Pos_x",p.x),
                (objectName + "Pos_y",p.y),
                (objectName + "Pos_z",p.z),
                (objectName + "Vel_x",v.x),
                (objectName + "Vel_y",v.y),
                (objectName + "Vel_z",v.z),
                (objectName + "Rot_x",r.x),
                (objectName + "Rot_y",r.y),
                (objectName + "Rot_z",r.z),
                (objectName + "MeshRadius",meshRadius),
                (objectName + "ColliderRadius",colliderRadius)
                
            };

            return values;
        }
    }
}
