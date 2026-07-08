

namespace SimpeLanguage.Project
{
    CompileFileData
    {
        public enum ECompileState
        {
            Default = 0,
            Ignore
        }

        CompileFileDataUnit
        {
            get set path = ""
            get set group = "";
            get set ECompileState compileState = ECompileState.Default
            get set int priority = 0

            _init_(MetaMemberData mmd )
            {

            }
        }

        List<CompileFileDataUnit> compileFileDataUnitList = new()

        Parse( MetaMemberData mmd )
        {
            for v in mmd.metaMemberDataDict
            {

            }
        }
    }

    ProjectManager
    {
        static void run( string path )
        {
            int index = path.lastIndexOf('\');
            if index != -1
            {
                rootPath = path.subString(0,index)
            }

            
        }
    }
}