using UnityEngine;
using System.Collections;

public class BulletScript : MonoBehaviour 
{
    public static int BulletCollisionLayers
    {
        get
        {
            return (1<<LayerMask.NameToLayer("Default")) |
                   (1<<LayerMask.NameToLayer("Player Hit"));
        }
    }

    public GameObject bulletCasingPrefab;
    public float speed = 900;
	float lifetime = 3;
    public int damage = 1;
    public float areaOfEffect = 0;
    public float homing = 0;
    public Transform target;
    bool dead;

    public NetworkPlayer Player { get; set; }

    void Awake()
    {
        GameObject casing = (GameObject)
            Instantiate(bulletCasingPrefab, transform.position, transform.rotation);
        casing.rigidbody.AddRelativeForce(
            new Vector3(1 + Random.value, Random.value, 0),
            ForceMode.Impulse);
        casing.rigidbody.AddTorque(
            5 * new Vector3(-0.5f-Random.value, -Random.value*0.1f, -0.5f-Random.value),
            ForceMode.Impulse);
    }

    bool DoDamageTo(Transform t)
    {
        HealthScript health = t.GetComponent<HealthScript>();
        // err, kinda lame, this is so that the collider can be
        // directly inside the damaged object rather than on it,
        // useful when the damage collider is different from the
        // real collider
        if(health == null && t.parent != null)
           health = t.parent.GetComponent<HealthScript>();

        if(health != null)
        {
            health.networkView.RPC(
                "DoDamage", RPCMode.Others, damage, Network.player);
            return true;
        }
        return false;
    }

	void Update()
    {
        if (!dead)
        {
            float distance = speed * Time.deltaTime;

            RaycastHit hitInfo;
            Physics.Raycast(transform.position, transform.forward, out hitInfo,
                            distance, BulletCollisionLayers);

            if (hitInfo.transform)
            {
                if (Player == Network.player)
                {
                    if (hitInfo.transform != null &&
                        (hitInfo.transform.networkView == null ||
                         hitInfo.transform.networkView.owner != Network.player))
                    {
                        bool playerHit = false;
                        if (areaOfEffect > 0)
                        {
                            Collider[] colliders = Physics.OverlapSphere(
                                hitInfo.point, areaOfEffect,
                                (1 << LayerMask.NameToLayer("Player Hit")));
                            foreach (Collider c in colliders)
                            {
                                playerHit = playerHit || DoDamageTo(c.transform);
                            }
                        }
                        else
                        {
                            playerHit = DoDamageTo(hitInfo.transform);
                        }

                        string effect = playerHit ? "ExplosionHit" : "Explosion";
                        if (areaOfEffect > 0)
                        {
                            effect += "Area";
                        }
                        EffectsScript.DoEffect(
                            effect,
                            hitInfo.point,
                            Quaternion.LookRotation(hitInfo.normal));

                        dead = true;
                        renderer.enabled = false;
                    }
                }
                else
                {
                    dead = true;
                    renderer.enabled = false;
                }
            }

            transform.position += transform.forward * distance;

            // homing
            if (target != null && homing > 0)
            {
                //Debug.Log("Is homing @ " + homing);
                var lookVec = (target.position - transform.position).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookVec),
                                                      Mathf.Clamp01(homing * Time.deltaTime * 4));
            }
        }

	    var o = lifetime / 3f * 0.75f;
        GetComponent<TrailRenderer>().material.SetColor("_TintColor", new Color(o, o, o, 1));

	    // max lifetime
		lifetime -= Time.deltaTime;
		if (lifetime <= 0)
			Destroy(gameObject);
	}
}
