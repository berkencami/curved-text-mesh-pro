using UnityEngine;
using TMPro;

namespace CurvedTextMeshPro
{
    /// <summary>
    /// A component that transforms TextMeshPro text into a curved/arc shape.
    /// The text can be adjusted from completely flat (0 degrees) to a full half-circle (180 degrees).
    /// </summary>
    [ExecuteInEditMode]
    public class CurvedTextMeshPro : MonoBehaviour
    {
        [SerializeField]
        [Range(0, 180)] 
        [Tooltip("The arc angle of the text. 0 = flat text, 180 = half circle")]
        private float _arcDegrees = 0.0f;

        // Reference to the TextMeshPro component
        private TMP_Text textMeshPro;
        // Flag to force mesh update when needed
        private bool _forceUpdate;
        // Stores the previous arc degrees value for change detection
        private float _oldArcDegrees = float.MaxValue;

        /// <summary>
        /// Initializes the component by getting the TextMeshPro reference
        /// </summary>
        private void Awake()
        {
            textMeshPro = gameObject.GetComponent<TMP_Text>();
        }

        /// <summary>
        /// Called when the component is enabled. Forces an initial mesh update
        /// </summary>
        private void OnEnable()
        {
            _forceUpdate = true;
            UpdateMesh();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Updates the mesh in edit mode to show real-time changes
        /// </summary>
        private void Update()
        {
            UpdateMesh();
        }
#endif

        /// <summary>
        /// Main method that updates the text mesh to create the curved effect.
        /// This method:
        /// 1. Updates the text mesh
        /// 2. Calculates the center point
        /// 3. Applies transformations to each character
        /// 4. Maintains the original center position
        /// </summary>
        private void UpdateMesh()
        {
            // Skip update if no changes detected
            if (!_forceUpdate && !textMeshPro.havePropertiesChanged && !ParametersHaveChanged())
            {
                return;
            }

            _forceUpdate = false;

            // Force TextMeshPro to update its mesh
            textMeshPro.ForceMeshUpdate();

            var textInfo = textMeshPro.textInfo;
            var characterCount = textInfo.characterCount;

            if (characterCount == 0)
                return;

            var boundsMinX = textMeshPro.bounds.min.x;
            var boundsMaxX = textMeshPro.bounds.max.x;
            var boundsMinY = textMeshPro.bounds.min.y;
            var boundsMaxY = textMeshPro.bounds.max.y;
            
            Vector3 textCenter = new Vector3(
                (boundsMinX + boundsMaxX) * 0.5f,
                (boundsMinY + boundsMaxY) * 0.5f,
                0
            );

            // If arcDegrees is 0, skip all transformation and leave characters as is
            if (_arcDegrees == 0)
            {
                textMeshPro.UpdateVertexData();
                return;
            }

            // Transform each character to create the curved effect
            for (var i = 0; i < characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                var vertexIndex = textInfo.characterInfo[i].vertexIndex;
                var materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                var vertices = textInfo.meshInfo[materialIndex].vertices;

                // Calculate the middle baseline position of the character
                Vector3 charMidBaselinePos = new Vector2(
                    (vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2, 
                    textInfo.characterInfo[i].baseLine);

                // Center the character vertices
                vertices[vertexIndex + 0] += -charMidBaselinePos;
                vertices[vertexIndex + 1] += -charMidBaselinePos;
                vertices[vertexIndex + 2] += -charMidBaselinePos;
                vertices[vertexIndex + 3] += -charMidBaselinePos;

                // Calculate the position of the character in the text (0 to 1)
                var zeroToOnePos = (charMidBaselinePos.x - boundsMinX) / (boundsMaxX - boundsMinX);

                // Get the transformation matrix for this character
                var matrix = ComputeCircleTransformationMatrix(zeroToOnePos, textInfo, i);

                // Apply the transformation to each vertex of the character
                vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
                vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
                vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
                vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);
            }
            
            // Calculate the new center after transformation
            var newCenter = Vector3.zero;
            var totalVertices = 0;
            
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                if (textInfo.meshInfo[i].vertices != null && textInfo.meshInfo[i].vertexCount > 0)
                {
                    for (int j = 0; j < textInfo.meshInfo[i].vertexCount; j++)
                    {
                        newCenter += textInfo.meshInfo[i].vertices[j];
                        totalVertices++;
                    }
                }
            }
            
            // Adjust vertices to maintain the original center position
            if (totalVertices > 0)
            {
                newCenter /= totalVertices;
                
                // Calculate the offset needed to maintain the original center
                var offset = textCenter - newCenter;
                
                // Apply the offset to all vertices
                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    if (textInfo.meshInfo[i].vertices != null && textInfo.meshInfo[i].vertexCount > 0)
                    {
                        for (int j = 0; j < textInfo.meshInfo[i].vertexCount; j++)
                        {
                            textInfo.meshInfo[i].vertices[j] += offset;
                        }
                    }
                }
            }

            // Update the mesh with the new vertex positions
            textMeshPro.UpdateVertexData();
        }

        /// <summary>
        /// Checks if any parameters have changed since the last update
        /// </summary>
        /// <returns>True if parameters have changed, false otherwise</returns>
        private bool ParametersHaveChanged()
        {
            var retVal = !Mathf.Approximately(_arcDegrees, _oldArcDegrees);
            _oldArcDegrees = _arcDegrees;
            return retVal;
        }

        /// <summary>
        /// Computes the transformation matrix for a character based on its position in the text
        /// </summary>
        /// <param name="zeroToOnePos">The position of the character in the text (0 to 1)</param>
        /// <param name="textInfo">The TextMeshPro text information</param>
        /// <param name="charIdx">The index of the character</param>
        /// <returns>A transformation matrix that positions and rotates the character</returns>
        private Matrix4x4 ComputeCircleTransformationMatrix(float zeroToOnePos, TMP_TextInfo textInfo, int charIdx)
        {
            // If arc degrees is 0, return identity matrix (flat text)
            if (_arcDegrees == 0)
            {
                return Matrix4x4.identity;
            }

            // Calculate radius based on text width and arc angle
            var textWidth = textInfo.lineInfo[0].lineExtents.max.x - textInfo.lineInfo[0].lineExtents.min.x;
            var radius = textWidth / (2 * Mathf.Sin(_arcDegrees * 0.5f * Mathf.Deg2Rad));
            
            // Calculate the angle for this character
            var angle = ((zeroToOnePos - 0.5f) * _arcDegrees - 90) * Mathf.Deg2Rad;
            
            // Calculate the position on the circle
            var x0 = Mathf.Cos(angle);            
            var y0 = Mathf.Sin(angle);
            
            // Adjust radius for multi-line text
            var radiusForThisLine =
                radius - textInfo.lineInfo[0].lineExtents.max.y * textInfo.characterInfo[charIdx].lineNumber;
            
            // Calculate the new position
            var newMidBaselinePos = new Vector2(x0 * radiusForThisLine, -y0 * radiusForThisLine);

            // Create and return the transformation matrix
            return Matrix4x4.TRS(
                new Vector3(newMidBaselinePos.x, newMidBaselinePos.y, 0), 
                Quaternion.AngleAxis(-Mathf.Atan2(y0, x0) * Mathf.Rad2Deg - 90, Vector3.forward), 
                Vector3.one);
        }
    }
}