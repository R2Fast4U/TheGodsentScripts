using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launcher : MonoBehaviour
{
    // Declare bulletPrefab as a public variable
    public GameObject bulletPrefab;
    public GameObject shooter;
    private Transform _firePoint;

    private void Awake ()
    {
        _firePoint = transform.Find("FirePoint");
    }

    // Start is called before the first frame update
    void Start()
    {
        // Instantiate the bulletPrefab at the position and rotation of the Launcher
        Invoke ("Shoot", 1f);
        Invoke ("Shoot", 2f);
        Invoke ("Shoot", 3f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Shoot ()
    {
        if (bulletPrefab != null && _firePoint != null && shooter != null){
            GameObject myBullet = Instantiate(bulletPrefab, _firePoint.position, Quaternion.identity) as GameObject;

            Bullet bulletComponent = myBullet.GetComponent<Bullet>();

            if (shooter.transform.localScale.x < 0f) {
                bulletComponent.direction = Vector2.left;

            }else {
               bulletComponent.direction = Vector2.right; 
            }
        }

    }
}
