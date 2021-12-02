using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Monster : MonoBehaviour
{
    public EnemyZone spawnZone;
    public NavMeshAgent agent;
    public static Dictionary<int, Monster> monsters = new Dictionary<int, Monster>();
    public int id = 0;
    public float moveSpeed = 2f;
    public float health = 20f;
    public float chaseDistance = 10f;
    public float attackDistance = 0.4f;
    public float roamDistance = 30f;
    public Player targetPlayer = null;
    public Vector3 spawnPosition;
    public MonsterStates monsterStates = MonsterStates.Roam;

    public void Start()
    {
        spawnPosition = transform.position;
        agent.speed = moveSpeed;
        while(monsters.ContainsKey(id))
        {
            id++;
        }
        monsters.Add(id, this);
        Sender.PopulateMonster(id, transform.position);
    }

    public void FixedUpdate()
    {
        switch(monsterStates)
        {
            case MonsterStates.Roam:
                Roam();
                break;
            case MonsterStates.Return:
                Return();
                break;
            case MonsterStates.Die:
                Die();
                break;
            case MonsterStates.Chase:
                Chase();
                break;
            case MonsterStates.Attack:
                break;
        }
        Sender.MonsterTransform(this);
    }

    public virtual void Die()
    {
        spawnZone.MonsterDeath(this);
        Sender.MonsterDeath(id);
        monsters.Remove(id);
        Destroy(this.gameObject);
    }

    public virtual void Roam()
    {
        if(agent.destination == null||agent.remainingDistance < 0.1f)
        {
            agent.SetDestination(GetNavMeshLocation(3f));
        }

        foreach (Client client in ServerLogic.clients.Values)
        {
            Player player = client.player;
            if(player == null)
            {
                continue;
            }
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < chaseDistance)
            {
                if(targetPlayer == null || distance < Vector3.Distance(targetPlayer.transform.position, transform.position))
                {
                    targetPlayer = player;
                }
            }
        }

        if(targetPlayer != null)
        {
            monsterStates = MonsterStates.Chase;
        }

    }

    public virtual void Chase()
    {
        Debug.Log("Chase");
        if(Vector3.Distance(transform.position, targetPlayer.transform.position) > chaseDistance)
        {
            targetPlayer = null;
            monsterStates = MonsterStates.Return;
        }
        else
        {
            agent.SetDestination(targetPlayer.transform.position);
            if(agent.remainingDistance < attackDistance)
            {
                monsterStates = MonsterStates.Attack;
                return;
            }
            if(Vector3.Distance(spawnPosition, transform.position) > chaseDistance)
            {
                targetPlayer = null;
                monsterStates = MonsterStates.Return;
                return;
            }
        }
    }

    public virtual void Return()
    {
        agent.SetDestination(spawnPosition);
        if(agent.remainingDistance < 1f)
        {
            monsterStates = MonsterStates.Roam;
        }
    }

    private Vector3 GetNavMeshLocation(float _radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * _radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, _radius, 1);
        Vector3 finalPosition = hit.position;
        return finalPosition;
    }
}

public enum MonsterStates
{
    Roam,
    Attack,
    Chase,
    Return,
    Die
}
