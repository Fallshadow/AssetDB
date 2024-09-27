public abstract class BaseHandler {
    protected abstract void initEvents();
    protected abstract void releaseEvents();

    public void Init() {
        initEvents();
    }

    public void Release() {
        
    }
}