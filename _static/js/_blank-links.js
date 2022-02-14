// https://stackoverflow.com/questions/11716781/open-a-link-in-a-new-window-in-restructuredtext

$(document).ready(function () {
    $('a[href^="http://"], a[href^="https://"]').not('a[class*=internal]').attr('target', '_blank');
});