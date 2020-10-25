var video = document.querySelector("#video");

// Basic settings for the video to get from Webcam
const constraints = {
    audio: false,
    video: {
        width: 475, height: 475
    }
};

// This condition will ask permission to user for Webcam access
if (navigator.mediaDevices.getUserMedia) {
    navigator.mediaDevices.getUserMedia(constraints)
        .then(function (stream) {
            video.srcObject = stream;

            console.log(stream.getVideoTracks())
            console.log(stream.getVideoTracks()[0].contentHint)
        })
        .catch(function (err0r) {
            console.log("Error while getting userMedia: " + err0r);
        });
}

$("#btnCapture").click(function () {
    var canvas = document.getElementById('canvas');
    var context = canvas.getContext('2d');

    console.log("Hey")
    console.log(video)

    video.

    // Capture the image into canvas from Webcam streaming Video element
    context.drawImage(video, 0, 0);
});
