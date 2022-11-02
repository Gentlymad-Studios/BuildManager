namespace BuildManager
{
    public interface IModuleBase
    {
        void Draw();
        void Save();
    }

    public abstract class TargetGroupDependentModuleBase : IModuleBase
    {
        protected TargetGroupModule targetGroupModule;
        protected string TypeName
        {
            get
            {
                if (_TypeName == null)
                {
                    _TypeName = GetType().ToString();
                }
                return _TypeName;
            }
        }
        private string _TypeName = null;

        public TargetGroupDependentModuleBase(TargetGroupModule targetGroupModule)
        {
            this.targetGroupModule = targetGroupModule;
        }

        public abstract void Draw();
        public abstract void Save();
    }
}

