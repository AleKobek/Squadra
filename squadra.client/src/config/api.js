const getOrigin = () => {
    if (typeof window !== "undefined" && window.location?.origin) {
        return window.location.origin;
    }

    return "http://localhost:3000";
};

const getApiBaseUrl = () => {
    if (typeof window !== "undefined" && window.location?.hostname) {
        return `${window.location.protocol}//${window.location.hostname}:5014/api`;
    }

    return "http://localhost:5014/api";
};

export const API_BASE_URL = getApiBaseUrl();
export const CLIENT_URL = getOrigin();
