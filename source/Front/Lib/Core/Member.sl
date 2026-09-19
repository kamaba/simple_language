
public class Member extends Object
{
    public string name = "";
    public int index = -1;
    public object value = null;
    override void _init_()
    {
    }
    public override string toString()
    {
        string nameText = this.name == null ? "" : this.name
        string indexText = this.index.toString()
        string valueText = this.value == null ? "null" : this.value.toString()
        ret "Member{name=" + nameText + ", index=" + indexText + ", value=" + valueText + "}"
    }
}
