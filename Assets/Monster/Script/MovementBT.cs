using StarterAssets;
using System;
using System.Collections.Generic;
using UnityEngine;


public enum NodeState
{
    Running,
    Success,
    Failure
}

public abstract class Node
{
    public abstract NodeState Evaluate();
}

public class SelectorNode : Node
{
    private List<Node> childNodes;

    public SelectorNode(List<Node> nodes)
    {
        childNodes = nodes;
    }

    public override NodeState Evaluate()
    {
        foreach (Node node in childNodes)
        {
            NodeState result = node.Evaluate();
            if (result == NodeState.Success)
                return NodeState.Success;
            else if (result == NodeState.Running)
                return NodeState.Running;
        }
        return NodeState.Failure;
    }

}

public class SequenceNode : Node
{
    private List<Node> childNodes;

    public SequenceNode(List<Node> nodes)
    {
        childNodes = nodes;
    }

    public override NodeState Evaluate()
    {
        bool allChildrenSuccess = true;
        foreach (Node node in childNodes)
        {
            NodeState result = node.Evaluate();
            if (result == NodeState.Failure)
                return NodeState.Failure;
            else if (result == NodeState.Running)
                allChildrenSuccess = false;
        }
        return allChildrenSuccess ? NodeState.Success : NodeState.Running;
    }
}

public class CkPlayerState : Node
{
    private ThirdPersonController playerMove;

    public CkPlayerState(ThirdPersonController movement)
    {
        playerMove = movement;
    }

    public override NodeState Evaluate()
    {
        if (playerMove.IsMoving())
        {
            return NodeState.Success;
        }
        else
        {
            return NodeState.Failure;
        }
    }
}

public class GotoTarget : Node
{
    private Func<NodeState> action;

    public GotoTarget(Func<NodeState> action)
    {
        this.action = action;
    }

    public override NodeState Evaluate()
    {
        return action.Invoke();
    }
}

public class MonsterRun : Node
{
    private Transform agentTransform;
    private Transform playerTransform;
    private float moveSpd = 2.0f;
    private float fleeDistance = 10.0f;

    private bool fleeing = false;

    public MonsterRun(Transform agent, Transform player)
    {
        agentTransform = agent;
        playerTransform = player;
        fleeing = false;
    }
    public override NodeState Evaluate()
    {
        Vector3 agentPosition = agentTransform.position;
        Vector3 playerPosition = playerTransform.position;
        Vector3 dirToPlayer = playerPosition - agentPosition;
        float _distance = dirToPlayer.magnitude;

        if (!fleeing)
        {
            Vector3 dirAvoidPlayer = -dirToPlayer.normalized;
            Vector3 vecMove = dirAvoidPlayer * moveSpd * Time.deltaTime;

            if (_distance < fleeDistance)
            {
                fleeing = false;
                return NodeState.Running;
            } else
            {
                agentTransform.Translate(vecMove);
                return NodeState.Running;
            }
        }
        else
        {
            Vector3 dirOpposite
                = (agentPosition - playerPosition).normalized;

            Vector3 moveVector = dirOpposite * moveSpd * Time.deltaTime;
            agentTransform.Translate(moveVector);

            return NodeState.Success;
        }
    }
}

public class Movement
{
    private Transform followerTransform;
    private float followDistance;
    private float maxSpd = 10.0f;
    private float nowSpd = 0.0f;
    private float addSpd = 2.0f;

    public Movement(Transform follower, float distance)
    {
        followerTransform = follower;
        followDistance = distance;
    }
    public NodeState FollowPlayerAction(Transform playerTransform)
    {
        Vector3 direction
            = playerTransform.position - followerTransform.position;
        float distance = direction.magnitude;

        float targetSpd
            = Mathf.Lerp(0,
                         maxSpd,
                         Mathf.InverseLerp(0, followDistance, distance)
                         );
        nowSpd = Mathf.MoveTowards(nowSpd, targetSpd, addSpd * Time.deltaTime);
        Vector3 moveVector
            = direction.normalized * nowSpd * Time.deltaTime;

        followerTransform.Translate(moveVector);

        if (distance > followDistance)
        {
            return NodeState.Running;
        }
        else
        {
            nowSpd = 0.0f;
            return NodeState.Success;
        }
    }
}

public class MovementBT : MonoBehaviour
{
    private Node root;
    private Movement movement;

    private void Update()
    {
        root.Evaluate();
    }

    private void Start()
    {
        Transform followerTransform = transform;
        float followDistance = 2.0f;
        movement = new Movement(followerTransform, followDistance);

        ThirdPersonController playerMove
            = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonController>();

        root = new SelectorNode(new List<Node>
        {
            new SequenceNode(new List<Node>
            {
                new CkPlayerState(playerMove),
                new GotoTarget(() => movement.FollowPlayerAction(playerMove.transform))
            }),
            new MonsterRun(followerTransform, playerMove.transform),
        });
    }
}

