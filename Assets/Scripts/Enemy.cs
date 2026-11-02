using TMPro;
using UnityEngine;

public class Enemy : CellObject
{
    public int Health = 3;

    private int m_CurrentHealth;

    private Animator m_Animator;

    private Vector3 m_MoveTarget;

    private bool m_IsMoving;

    public float MoveSpeed = 5;

    private void Awake()
    {
        GameManager.Instance.TurnManager.OnTick += TurnHappened;

        m_Animator = GetComponent<Animator>();
    }

    private void OnDestroy()
    {
        GameManager.Instance.TurnManager.OnTick -= TurnHappened;
    }

    public override void Init(Vector2Int coord)
    {
        base.Init(coord);
        m_CurrentHealth = Health;
    }

    public override bool PlayerWantsToEnter()
    {
        m_CurrentHealth -= 1;

        if (m_CurrentHealth <= 0)
        {
            Destroy(gameObject);
        }

        return false;
    }

    void Update()
    {
        if(m_IsMoving)
        {
            
            transform.position = Vector3.MoveTowards(transform.position, m_MoveTarget, MoveSpeed * Time.deltaTime);

            if (transform.position == m_MoveTarget)
            {
                m_IsMoving = false;
                m_Animator.SetBool("Moving", false);
                GameManager.Instance.TurnManager.Waiting = m_IsMoving;
            }

            return;
        }
    }

    bool MoveTo(Vector2Int coord, bool immediate)
    {
        var board = GameManager.Instance.BoardManager;
        var targetCell = board.GetCellData(coord);

        if (targetCell == null
            || !targetCell.Passable
            || targetCell.ContainedObject != null)
        {
            return false;
        }

        //remove enemy from current cell
        var currentCell = board.GetCellData(m_Cell);
        currentCell.ContainedObject = null;

        //add it to the next cell
        targetCell.ContainedObject = this;
        m_Cell = coord;

        if (immediate)
        {
            m_IsMoving = false;
            transform.position = board.CellToWorld(m_Cell);
        }
        else
        {
            m_IsMoving = true;
            m_MoveTarget = board.CellToWorld(m_Cell);
        }

        GameManager.Instance.TurnManager.Waiting = m_IsMoving;
        m_Animator.SetBool("Moving", m_IsMoving);

        return true;
    }

    void TurnHappened()
    {
        //We added a public property that return the player current cell!
        var playerCell = GameManager.Instance.PlayerController.Cell;

        int xDist = playerCell.x - m_Cell.x;
        int yDist = playerCell.y - m_Cell.y;

        int absXDist = Mathf.Abs(xDist);
        int absYDist = Mathf.Abs(yDist);

        if ((xDist == 0 && absYDist == 1)
            || (yDist == 0 && absXDist == 1))
        {
            //we are adjacent to the player, attack!
            GameManager.Instance.ChangeFood(-3);
            m_Animator.SetTrigger("Attack");
        }
        else
        {
            if (absXDist > absYDist)
            {
                if (!TryMoveInX(xDist))
                {
                    //if our move was not successful (so no move and not attack)
                    //we try to move along Y
                    TryMoveInY(yDist);
                }
            }
            else
            {
                if (!TryMoveInY(yDist))
                {
                    TryMoveInX(xDist);
                }
            }
        }
    }

    bool TryMoveInX(int xDist)
    {
        //try to get closer in x

        //player to our right
        if (xDist > 0)
        {
            return MoveTo(m_Cell + Vector2Int.right, false);
        }

        //player to our left
        return MoveTo(m_Cell + Vector2Int.left, false);
    }

    bool TryMoveInY(int yDist)
    {
        //try to get closer in y

        //player on top
        if (yDist > 0)
        {
            return MoveTo(m_Cell + Vector2Int.up, false);
        }

        //player below
        return MoveTo(m_Cell + Vector2Int.down, false);
    }
}
