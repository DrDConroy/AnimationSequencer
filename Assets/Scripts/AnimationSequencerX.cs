using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AnimationSequencerX : MonoBehaviour{

    //Animation Variables
    //Test Sequence
    public AnimationSequenceX[] animationSequence;
    public Queue<AnimationSequenceX> animationQueue = new Queue<AnimationSequenceX>();
    AnimationSequenceX thisAnim;
    public AnimStateX animState;

    //NPC
    public Vector3 moveLoc;
    public GameObject NPC;
    public float NPCSpeed = 2.5f;
    public Animator NPCanims;
    public Rig NPCRHRig;
    public Rig NPCLookRig;
    public GameObject targetObject; //Global
    public GameObject gestureLoc;   //NPC 
    public GameObject lookLoc;      //NPC
    private float accuracy = 0.1f;

    //Animation Timing
    private float animTimer;
    private bool timedAnim = false;

    //NPC Aiming Objects
    private Vector3 aimDirection;
    private Vector3 headDirection;
    public LineRenderer linePointer;

    //Audio Dialogue
    public AudioSource dialogueAudio;
    public Queue<AudioClip> dialogQueue = new Queue<AudioClip>();

    // Start is called before the first frame update
    void Start() {
        animState = AnimStateX.Idle;

        //Testing purposes - load test sequence on startup
        for (int i = 0; i < animationSequence.Length; i++) {
            animationQueue.Enqueue(animationSequence[i]);
        }
    }

    // Update is called once per frame
    void Update() {

        //Turn Animation Couroutine on/off
        if (animationQueue.Count > 0)
            StartCoroutine(AnimationSequencer());
        else if (animationQueue.Count == 0)
            StopCoroutine(AnimationSequencer());

        //Turn Audio Couroutine on/off
        if (dialogQueue.Count > 0)
            StartCoroutine(DialogueAudio());
        else
            StopCoroutine(DialogueAudio());

        //Timed animation control - forced return to Idle
        if (animTimer < Time.time && timedAnim) {
            timedAnim = false;
            animState = AnimStateX.Idle;
        }

        AnimationStates();
    }

    //Simple State Machine for controlling animation states
    private void AnimationStates() {

        //Animation Handling States
        switch (animState) {

            case AnimStateX.Idle:
                NPCanims.SetBool("Idle", true);
                NPCanims.SetBool("RHPointHold", false);
                NPCanims.SetBool("Walk", false);
                NPCRHRig.weight = 0;
                NPCLookRig.weight = 0;

                //Body Rotation
                RotateBodyNoYAxis(lookLoc.transform.position, 5);

                //Effects disable
                if (targetObject != null) {
                    if (targetObject.GetComponent<Outline>() != null)
                        targetObject.GetComponent<Outline>().enabled = false;

                    targetObject = null;

                    linePointer.enabled = false;
                }
                break;

            //Turn and Walk
            case AnimStateX.Walk:

                //Walking - If not near moveLoc - Turn, Walk towards it, then return to Idle
                if (Vector3.Distance(transform.position, moveLoc) > 0.15f) {

                    RotateBodyNoYAxis(moveLoc, 5);
                    transform.position = Vector3.MoveTowards(transform.position, moveLoc, NPCSpeed * 2 * Time.deltaTime);
                    NPCanims.SetBool("Walk", true);

                }
                //Idle if near target
                else if (Vector3.Distance(transform.position, moveLoc) < 0.15f) {
                    //Force return to Idle
                    NPCanims.SetBool("Walk", false);
                    animState = AnimStateX.Idle;
                    NPCanims.SetBool("Idle", true);
                }
                break;

            case AnimStateX.Wave:
                NPCanims.SetBool("Idle", false);
                NPCanims.SetBool("Wave Left", true);

                NPCLookRig.weight = 1;

                //Body Rotation
                RotateBodyNoYAxis(lookLoc.transform.position, 5);
                break;

            case AnimStateX.RHPoint:
                NPCanims.SetBool("Idle", false);
                NPCanims.SetBool("RHPoint", true);

                NPCLookRig.weight = 1;

                //Body Rotation
                RotateBodyNoYAxis(gestureLoc.transform.position, 5);
                break;

            // Instructional use - point at and highlight specific gameobjects
            case AnimStateX.RHPointHold:
                NPCanims.SetBool("Idle", false);
                NPCanims.SetBool("RHPointHold", true);

                NPCRHRig.weight = 1;
                NPCLookRig.weight = 1;

                //Outlining
                if (targetObject != null) {
                    if (targetObject.GetComponent<Outline>() != null)
                        targetObject.GetComponent<Outline>().enabled = true;
                }

                //Line Renderer
                if (targetObject != null) {
                    linePointer.enabled = true;
                    linePointer.SetPosition(0, linePointer.transform.position);
                    linePointer.SetPosition(1, targetObject.transform.position);
                }

                //Body Rotation
                RotateBodyNoYAxis(moveLoc, 5);
                break;

            case AnimStateX.AcknowledgeRH:
                NPCanims.SetBool("Idle", false);
                NPCanims.SetBool("Acknowledge RH", true);

                //Body Rotation
                RotateBodyNoYAxis(gestureLoc.transform.position, 5);
                break;

            case AnimStateX.Talking:
                NPCanims.SetBool("Idle", false);
                NPCanims.SetBool("Talking", true);

                //Body Rotation
                RotateBodyNoYAxis(gestureLoc.transform.position, 5);
                break;

        }
    }

    private void RotateBodyNoYAxis(Vector3 targetLocation, float speed) {

        //Ensure target is flat on Y axis
        targetLocation.y = 0;
        //targetLocation.y = -1.719f;

        //Rotate towards adjusted vector
        Vector3 direction = targetLocation - NPC.transform.position;
        Vector3 newDirection = Vector3.RotateTowards(NPC.transform.forward, direction, speed * Time.deltaTime, 0.0f);
        NPC.transform.rotation = Quaternion.LookRotation(newDirection);

        Vector3 youDir = transform.right;
        Vector3 waypointDir = moveLoc - transform.position;
        float dotProduct = Vector3.Dot(youDir, waypointDir);

    }

    private IEnumerator AnimationSequencer() {

        while (true) {

            if (animationQueue.Count > 0 && NPCanims.GetBool("Idle") == true && !timedAnim) {

                NPCanims.SetBool("Idle", false);

                thisAnim = animationQueue.Dequeue();

                //Set Animation States and Locations (including parent objects)
                animState = thisAnim.animState;

                //Move Location
                if(thisAnim.NPCLoc)
                    moveLoc = thisAnim.NPCLoc.transform.position;

                //Hand Gestures (Animation Rigging)
                if (thisAnim.NPCGestureLoc) {
                    targetObject = thisAnim.NPCGestureLoc;
                    gestureLoc.transform.position = thisAnim.NPCGestureLoc.transform.position;
                    gestureLoc.transform.parent = thisAnim.NPCGestureLoc.transform;
                }

                //Look Location (Animation Rigging)
                if (thisAnim.NPCLookLoc) {
                    lookLoc.transform.position = thisAnim.NPCLookLoc.transform.position;
                    lookLoc.transform.parent = thisAnim.NPCLookLoc.transform;
                }

                //GameObject Enables/Disables
                if (thisAnim.enables.Length > 0) {
                    foreach (GameObject gameObject in thisAnim.enables) {
                        if(!gameObject.active)
                            gameObject.SetActive(true);
                        else if(gameObject.active)
                            gameObject.SetActive(false);
                    }
                }

                //Audio
                if (thisAnim.dialogue)
                    dialogQueue.Enqueue(thisAnim.dialogue);

                //Enable duration logic check - useful for timed hold animations
                if (thisAnim.duration > 0.0f) {
                    animTimer = Time.time + thisAnim.duration;
                    timedAnim = true;
                }

            }
            yield return null;
        }
    }

    //Generic function to turn specific animations off
    public void ReturnToIdle(string animName) {
        NPCanims.SetBool(animName, false);
        animState = AnimStateX.Idle;
    }

    //Coroutine for playing audio in queue
    private IEnumerator DialogueAudio() {

        while (true) {

            if (dialogQueue.Count > 0) {

                if (dialogueAudio.isPlaying)
                    yield return new WaitForSeconds(dialogueAudio.clip.length);
                else {
                    dialogueAudio.clip = dialogQueue.Dequeue();
                    dialogueAudio.Play();
                }
            }
            yield return null;
        }
    }
}

public enum AnimStateX { Idle, Walk, Wave, RHPoint, RHPointHold, AcknowledgeRH, Talking };

[System.Serializable]
public class AnimationSequenceX {

    public AnimStateX animState;

    public GameObject NPCLoc;

    public GameObject NPCLookLoc;

    public GameObject NPCGestureLoc;

    public AudioClip dialogue;

    public GameObject[] enables;

    public float duration;
}
