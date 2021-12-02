using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{

    public EnemyState enemyState = EnemyState.Waiting;
    public float moveSpeed = 5f;
    public int damage = 1;

    public NavMeshAgent agent;
    public Animator animator;

    public float viewRange = 50f;
    public float attackRange = 20f;

    public float stunTimeLeft = 0;

    public Vector3 lastSeenPosition;
    public Vector3 originPosition;

    public float patrolChance = 0.3f;

    public float maxDistanceFromOrigin = 30f;

    public float turnChance = 0.1f;
    public int movesSinceTurn = 0;
    public bool canTurn = true;
    public int minMovesSinceTurn = 3;
    public float angleThreshold = 45f;

    public float attackCooldown = 3f;
    public float timeTillAttack = 0;

    public ThrowProjectile throwProjectile;


    // Start is called before the first frame update
    void Start()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        animator = gameObject.GetComponent<Animator>();
        originPosition = transform.position;
        agent.speed = moveSpeed;
    }

    // Update is called once per frame
    public virtual void Update()
    {
        switch(enemyState)
        {
            case EnemyState.Waiting:
                Waiting();
                break;
            case EnemyState.Stunned:
                Stunned();
                break;
            case EnemyState.Patrolling:
                Patrolling();
                break;
            case EnemyState.LookingForPlayer:
                LookingForPlayer();
                break;
            case EnemyState.Attacking:
                Attacking();
                break;
            case EnemyState.Returning:
                Returning();
                break;
            case EnemyState.Transforming:
                break;
        }
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, viewRange);
    }

    public virtual void Returning()
    {
        if (Vector3.Distance(transform.position, originPosition) > 3)
        {
            agent.SetDestination(originPosition);
            animator.SetFloat("Speed", 1f);
        }
        else
        {
            enemyState = EnemyState.Waiting;
            animator.SetFloat("Speed", 0f);
        }
    }

    public virtual void Waiting()
    {
        if (Vector3.Distance(transform.position, FoxCharacter.Instance.transform.position) < viewRange)
        {
            enemyState = EnemyState.Attacking;
            lastSeenPosition = FoxCharacter.Instance.transform.position;
            return;
        }
        else
        {
            float chance = Random.Range(0f, 1f);
            if (chance > (1 - patrolChance))
            {
                enemyState = EnemyState.Patrolling;
                animator.SetFloat("Speed", 0.5f);
                return;
            }
        }
        if (Vector3.Distance(transform.position, originPosition) > 3)
        {
            agent.SetDestination(originPosition);
            animator.SetFloat("Speed", 0.5f);
        }
    }

    public virtual void Stunned()
    {
        animator.SetFloat("Speed", 0f);
        if (stunTimeLeft > 0)
        {
            stunTimeLeft -= Time.deltaTime;
        }
        else
        {
            stunTimeLeft = 0;
            enemyState = EnemyState.Waiting;
        }
    }

    public virtual void Stun(float _stunTime)
    {
        stunTimeLeft += _stunTime;
        enemyState = EnemyState.Stunned;
        animator.SetFloat("Speed", 0f);
    }

    public virtual void Patrolling()
    {
        animator.SetFloat("Speed", 0.8f);
        if (Vector3.Distance(originPosition, transform.position) > maxDistanceFromOrigin)
        {
            enemyState = EnemyState.Returning;
            return;
        }
        Vector3 randomDirection = GetMoveDirection();
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, moveSpeed, 1);
        Vector3 finalPosition = hit.position;
        if(finalPosition.y > transform.position.y + 1 || finalPosition.y < transform.position.y - 1)
        {
            randomDirection = GetPointInTriangle(transform, true);
            NavMesh.SamplePosition(randomDirection, out hit, moveSpeed, 1);
            finalPosition = hit.position;
            return;
        }
        agent.SetDestination(finalPosition);
        if (Vector3.Distance(transform.position, FoxCharacter.Instance.transform.position) < viewRange)
        {
            enemyState = EnemyState.Attacking;
            lastSeenPosition = FoxCharacter.Instance.transform.position;
            return;
        }
    }

    public virtual void LookingForPlayer()
    {
        if (Vector3.Distance(originPosition, transform.position) > maxDistanceFromOrigin)
        {
            enemyState = EnemyState.Returning;
            return;
        }
        if (Vector3.Distance(transform.position, FoxCharacter.Instance.transform.position) < viewRange)
        {
            enemyState = EnemyState.Attacking;
            lastSeenPosition = FoxCharacter.Instance.transform.position;
            return;
        }
        if (Vector3.Distance(transform.position, lastSeenPosition) > 3)
        {
            agent.SetDestination(lastSeenPosition);
        }
        else
        {
            enemyState = EnemyState.Waiting;
        }
    }

    public virtual void Attacking()
    {
        animator.SetFloat("Speed", 1f);
        if (Vector3.Distance(originPosition, transform.position) > maxDistanceFromOrigin)
        {
            enemyState = EnemyState.Returning;
            animator.SetBool("Attacking", false);
            return;
        }
        if (Vector3.Distance(transform.position, FoxCharacter.Instance.transform.position) > viewRange)
        {
            enemyState = EnemyState.LookingForPlayer;
            animator.SetBool("Attacking", false);
            return;
        }
        if(Vector3.Distance(transform.position, FoxCharacter.Instance.transform.position) > attackRange)
        {
            animator.SetBool("Attacking", false);
            agent.SetDestination(FoxCharacter.Instance.transform.position);
            lastSeenPosition = FoxCharacter.Instance.transform.position;
            return;
        }
        if(Vector3.Distance(transform.position, FoxCharacter.Instance.transform.position) < attackRange)
        {
            agent.SetDestination(transform.position);
            lastSeenPosition = FoxCharacter.Instance.transform.position;
            var lookPos = lastSeenPosition - transform.position;
            lookPos.y = 0;
            var rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 30);
            Attack();
        }
    }

    public virtual void Attack()
    {
        animator.SetBool("Attacking", true);
        if (timeTillAttack > attackCooldown)
        {
            if(animator.GetCurrentAnimatorStateInfo(0).normalizedTime - (int)animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.45f)
            {
                throwProjectile.Throw(FoxCharacter.Instance.gameObject.transform.position);
                timeTillAttack = 0;
            }
        }
        timeTillAttack += Time.deltaTime;
    }

    private Vector3 GetMoveDirection()
    {
        if (!canTurn && movesSinceTurn >= minMovesSinceTurn)
        {
            canTurn = true;
        }

        if (Random.Range(0f, 1f) < turnChance && canTurn)
        {
            Vector3 result = Random.insideUnitSphere * moveSpeed + transform.position;
            canTurn = false;
            movesSinceTurn = 0;
            if (Mathf.Abs(Vector3.Angle(result, transform.forward)) > angleThreshold)
            {
                return result;
            }
            return GetPointInTriangle(transform, Convert.ToBoolean(Random.Range(0,2))) * moveSpeed + transform.position;
        }
        else
        {
            movesSinceTurn++;
            return GetPointInTriangle(transform) * moveSpeed + transform.position;
        }
    }

    public Vector3 GetPointInTriangle(Transform _transform, bool _backwards = false)
    {
        Vector3 p1;
        Vector3 p2;
        Vector3 p3;
        if(_backwards)
        {
            p1 = _transform.position;
            p2 = _transform.position + _transform.right - _transform.forward;
            p3 = _transform.position - _transform.forward - _transform.right;
        }
        else
        {
            p1 = _transform.position;
            p2 = _transform.position + _transform.right + _transform.forward;
            p3 = _transform.position + _transform.forward - _transform.right;
        }

        float u1 = Random.Range(0f,1f);
        float u2 = Random.Range(0f, 1f);
        if(u1 + u2 > 1)
        {
            u1 = 1 - u1;
            u2 = 1 - u2;
        }

        Vector3 result = u1 * (p2 - p1) + u2 * (p3 - p1);
        return result;
    }

}

public enum EnemyState
{
    Waiting,
    Patrolling,
    LookingForPlayer,
    Attacking,
    Stunned,
    Returning,
    Transforming
}
