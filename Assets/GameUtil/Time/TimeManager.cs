using System.Collections.Generic;

public partial class TimeManager : SingletonMonoBehavior<TimeManager> {
    private LinkedList<Timer> timerLink = new LinkedList<Timer>();
    private LinkedListNode<Timer> tempCurrentNode = null;

    public void RemoveTimer(string name) {
        tempCurrentNode = timerLink.First;
        while (tempCurrentNode != null) {
            LinkedListNode<Timer> nextNode = tempCurrentNode.Next;

            if (tempCurrentNode.Value.Name == name) {
                timerLink.Remove(tempCurrentNode);
            }

            tempCurrentNode = nextNode;
        }
    }
}