using UnityEngine;

namespace BuildManager {
    public abstract class ValidatorBase : ScriptableObject, IValidator {
        public abstract bool Validate();
    }
}
