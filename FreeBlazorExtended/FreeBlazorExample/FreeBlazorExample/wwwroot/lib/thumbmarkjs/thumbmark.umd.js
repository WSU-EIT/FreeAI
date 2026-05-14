// Local stub for ThumbmarkJS (cdn.jsdelivr.net blocked in sandboxed environments).
// The real library is at https://github.com/thumbmarkjs/thumbmarkjs
// This stub provides the minimal API surface used by FreeBlazorExample:
//   ThumbmarkJS.setOption(key, value)  - no-op
//   ThumbmarkJS.getFingerprint()       - returns Promise<string> with a deterministic pseudo-fingerprint
// The fingerprint is derived from UA + screen + timezone so repeat visits match.
(function (global, factory) {
    if (typeof exports === 'object' && typeof module !== 'undefined') {
        module.exports = factory();
    } else if (typeof define === 'function' && define.amd) {
        define(factory);
    } else {
        global.ThumbmarkJS = factory();
    }
}(typeof self !== 'undefined' ? self : this, function () {
    'use strict';

    var _options = {};

    function _hash(str) {
        var h = 5381 | 0;
        for (var i = 0; i < str.length; i++) {
            h = ((h << 5) + h) ^ str.charCodeAt(i);
        }
        // 32-char hex string to look like a real fingerprint hash
        var hex = (h >>> 0).toString(16).padStart(8, '0');
        return hex + hex + hex + hex;
    }

    function setOption(key, value) {
        _options[key] = value;
    }

    function getFingerprint(includeData) {
        try {
            var parts = [
                navigator.userAgent || '',
                navigator.language || '',
                screen.width + 'x' + screen.height,
                screen.colorDepth || '',
                new Date().getTimezoneOffset(),
                navigator.hardwareConcurrency || '',
                navigator.platform || ''
            ].join('|');
            var fp = _hash(parts);
            return Promise.resolve(includeData === true ? { hash: fp, data: {} } : fp);
        } catch (e) {
            return Promise.resolve('00000000000000000000000000000000');
        }
    }

    return {
        setOption: setOption,
        getFingerprint: getFingerprint
    };
}));
