document.addEventListener("DOMContentLoaded", function () {

    function attendanceAction(options) {

        const button = document.getElementById(options.buttonId);

        if (!button)
            return;

        button.addEventListener("click", function () {

            button.disabled = true;

            if (options.textId) {

                document.getElementById(options.textId).innerHTML =
                    '<span class="spinner-border spinner-border-sm"></span> ' + options.processingText;

            }
            else {

                button.innerHTML =
                    '<span class="spinner-border spinner-border-sm"></span> ' + options.processingText;

            }

            fetch(options.url, {

                method: "POST"

            })

                .then(response => response.json())

                .then(data => {

                    Swal.fire({

                        icon: data.success ? "success" : "error",

                        title: data.title,

                        html: data.message.replace(/\n/g, "<br>"),

                        confirmButtonColor: "#198754"

                    }).then(() => {

                        if (data.success) {

                            location.reload();

                        }

                    });

                })

                .catch(error => {

                    button.disabled = false;

                    Swal.fire({

                        icon: "error",

                        title: "Error",

                        text: error.toString()

                    });

                });

        });

    }

    //========================================
    // Punch In
    //========================================

    attendanceAction({

        buttonId: "btnPunchIn",
        textId: "btnPunchInText",
        url: "/Attendance/PunchIn",
        processingText: "Processing..."

    });

    //========================================
    // Break
    //========================================

    attendanceAction({

        buttonId: "btnBreak",
        textId: null,
        url: "/Attendance/Break",
        processingText: "Starting Break..."

    });

    //========================================
    // Resume
    //========================================

    attendanceAction({

        buttonId: "btnResume",
        textId: null,
        url: "/Attendance/Resume",
        processingText: "Resuming..."

    });

    //========================================
    // Punch Out
    //========================================

    attendanceAction({

        buttonId: "btnPunchOut",
        textId: null,
        url: "/Attendance/PunchOut",
        processingText: "Punching Out..."

    });

});