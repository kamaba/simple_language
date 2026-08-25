
public class Transform extends Component {

    public Vector3 position = Vector3.zero;
    public Vector3 scale = Vector3.one;
    public Quaternion rotation = Quaternion.identity;

    public Transform parent = null;

    public get Vector3 forward(){
            ret rotation * Vector3.forward;
    }

    public get Vector3 right() {
            ret rotation * Vector3.right;
    }
}